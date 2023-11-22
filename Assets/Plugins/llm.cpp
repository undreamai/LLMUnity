#include "llm.h"

#include <cassert>
#include <cinttypes>
#include <cmath>
#include <cstdio>
#include <cstring>
#include <ctime>
#include <fstream>
#include <iostream>
#include <sstream>
#include <string>
#include <vector>

bool LLMParser::readline(std::string & line) {
    if (!std::getline(std::cin, line)) {
        // Input stream is bad or EOF received
        line.clear();
        return false;
    }
    bool ret = false;
    if (!line.empty()) {
        char last = line.back();
        if (last == '/') { // Always return control on '/' symbol
            line.pop_back();
            return false;
        }
        if (last == '\\') { // '\\' changes the default action
            line.pop_back();
            ret = true;
        }
    }
    line += '\n';

    // By default, continue input if multiline_input is set
    return ret;
}

std::vector<std::string> LLMParser::splitArguments(const std::string& inputString) {
    // Split the input string into individual arguments
    std::vector<std::string> arguments;

    unsigned counter = 0;
    std::string segment;
    std::istringstream stream_input(inputString);
    while(std::getline(stream_input, segment, '\"'))
    {
        ++counter;
        if (counter % 2 == 0)
        {
            if (!segment.empty()) arguments.push_back(segment);
        }
        else
        {
            std::istringstream stream_segment(segment);
            while(std::getline(stream_segment, segment, ' '))
                if (!segment.empty()) arguments.push_back(segment);
        }
    }
    return arguments;
}


gpt_params LLMParser::string_to_gpt_params(std::string params_string) {
    // add "llm" as the program name
    std::vector<std::string> arguments = splitArguments("llm " + params_string);

    // Convert vector of strings to argc and argv
    int argc = static_cast<int>(arguments.size());
    char** argv = new char*[argc];
    for (int i = 0; i < argc; ++i) {
        argv[i] = new char[arguments[i].size() + 1];
        std::strcpy(argv[i], arguments[i].c_str());
    }

    gpt_params params;
    if (!gpt_params_parse(argc, argv, params)) {
        throw std::runtime_error("could not parse the input parameters: " + params_string );
    }
    return params;
}



LLM::LLM(gpt_params params_in): params(params_in) {
    check_params();
    init();
}

LLM::LLM(std::string params_string) : LLM(LLMParser::string_to_gpt_params(params_string)){
}

void LLM::check_params(){
    if (params.logits_all) {
        printf("\n************\n");
        printf("%s: please use the 'perplexity' tool for perplexity calculations\n", __func__);
        printf("************\n\n");

        // TODO fail
        // return 0;
    }

    if (params.embedding) {
        printf("\n************\n");
        printf("%s: please use the 'embedding' tool for embedding calculations\n", __func__);
        printf("************\n\n");

        // TODO fail
        // return 0;
    }

    if (params.n_ctx != 0 && params.n_ctx < 8) {
        LOG_TEE("%s: warning: minimum context size is 8, using minimum size.\n", __func__);
        params.n_ctx = 8;
    }

    if (params.rope_freq_base != 0.0) {
        LOG_TEE("%s: warning: changing RoPE frequency base to %g.\n", __func__, params.rope_freq_base);
    }

    if (params.rope_freq_scale != 0.0) {
        LOG_TEE("%s: warning: scaling RoPE frequency by %g.\n", __func__, params.rope_freq_scale);
    }

    LOG_TEE("%s: build = %d (%s)\n",      __func__, LLAMA_BUILD_NUMBER, LLAMA_COMMIT);
    LOG_TEE("%s: built with %s for %s\n", __func__, LLAMA_COMPILER, LLAMA_BUILD_TARGET);

    if (params.seed == LLAMA_DEFAULT_SEED) {
        params.seed = time(NULL);
    }

    LOG_TEE("%s: seed  = %u\n", __func__, params.seed);

    std::mt19937 rng(params.seed);
    if (params.random_prompt) {
        params.prompt = gpt_random_prompt(rng);
    }
    // in instruct mode, we inject a prefix and a suffix to each input by the user
    if (params.instruct) {
        params.interactive_first = true;
        params.antiprompt.push_back("### Instruction:\n\n");
    }

    // enable interactive mode if interactive start is specified
    if (params.interactive_first) {
        params.interactive = true;
    }

    // print system information
    {
        LOG_TEE("\n");
        LOG_TEE("%s\n", get_system_info(params).c_str());
    }

    path_session = params.path_prompt_cache;
}

void LLM::load_model(){
    // load the model and apply lora adapter, if any
    LOG("%s: load the model and apply lora adapter, if any\n", __func__);
    std::tie(model, ctx) = llama_init_from_gpt_params(params);
    if (params.sparams.cfg_scale > 1.f) {
        struct llama_context_params lparams = llama_context_params_from_gpt_params(params);
        ctx_guidance = llama_new_context_with_model(model, lparams);
    }

    if (model == NULL) {
        LOG_TEE("%s: error: unable to load model\n", __func__);
        //TODO fail
        // return 1;
    }
}

void LLM::set_context_num(){
    const int n_ctx_train = llama_n_ctx_train(model);
    n_ctx = llama_n_ctx(ctx);
    LOG("n_ctx: %d\n", n_ctx);

    if (n_ctx > n_ctx_train) {
        LOG_TEE("%s: warning: model was trained on only %d context tokens (%d specified)\n",
                __func__, n_ctx_train, n_ctx);
    }
}

void LLM::load_saved_session(){
    if (!path_session.empty()) {
        LOG_TEE("%s: attempting to load saved session from '%s'\n", __func__, path_session.c_str());

        // fopen to check for existing session
        FILE * fp = std::fopen(path_session.c_str(), "rb");
        if (fp != NULL) {
            std::fclose(fp);

            session_tokens.resize(n_ctx);
            size_t n_token_count_out = 0;
            if (!llama_load_session_file(ctx, path_session.c_str(), session_tokens.data(), session_tokens.capacity(), &n_token_count_out)) {
                LOG_TEE("%s: error: failed to load session file '%s'\n", __func__, path_session.c_str());
                // TODO fail
                // return 1;
            }
            session_tokens.resize(n_token_count_out);
            llama_set_rng_seed(ctx, params.seed);

            LOG_TEE("%s: loaded a session with prompt size of %d tokens\n", __func__, (int) session_tokens.size());
        } else {
            LOG_TEE("%s: session file does not exist, will create\n", __func__);
        }
    }
}


void LLM::init_embedding_input(){
    if (params.interactive_first || params.instruct || !params.prompt.empty() || session_tokens.empty()) {
        LOG("tokenize the prompt\n");
        embd_inp = ::llama_tokenize(ctx, params.prompt, add_bos, true);
    } else {
        LOG("use session tokens\n");
        embd_inp = session_tokens;
    }

    LOG("prompt: \"%s\"\n", log_tostr(params.prompt));
    LOG("tokens: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, embd_inp).c_str());

    // Should not run without any tokens
    if (embd_inp.empty()) {
        embd_inp.push_back(llama_token_bos(model));
        LOG("embd_inp was considered empty and bos was added: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, embd_inp).c_str());
    }

    // TODO fail
    if ((int) embd_inp.size() > n_ctx - 4) {
        LOG_TEE("%s: error: prompt is too long (%d tokens, max %d)\n", __func__, (int) embd_inp.size(), n_ctx - 4);
        // return 1;
    }
}


void LLM::tokenize_negative_prompt(){
    if (ctx_guidance) {
        LOG("cfg_negative_prompt: \"%s\"\n", log_tostr(params.sparams.cfg_negative_prompt));

        guidance_inp = ::llama_tokenize(ctx_guidance, params.sparams.cfg_negative_prompt, add_bos, true);
        LOG("guidance_inp tokenized: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx_guidance, guidance_inp).c_str());

        std::vector<llama_token> original_inp = ::llama_tokenize(ctx, params.prompt, add_bos, true);
        LOG("original_inp tokenized: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, original_inp).c_str());

        original_prompt_len = original_inp.size();
        guidance_offset = (int)guidance_inp.size() - original_prompt_len;
        LOG("original_prompt_len: %s", log_tostr(original_prompt_len));
        LOG("guidance_offset:     %s", log_tostr(guidance_offset));
    }
}


void LLM::session_similarity(){
    n_matching_session_tokens = 0;
    if (!session_tokens.empty()) {
        for (llama_token id : session_tokens) {
            if (n_matching_session_tokens >= embd_inp.size() || id != embd_inp[n_matching_session_tokens]) {
                break;
            }
            n_matching_session_tokens++;
        }
        if (params.prompt.empty() && n_matching_session_tokens == embd_inp.size()) {
            LOG_TEE("%s: using full prompt from session file\n", __func__);
        } else if (n_matching_session_tokens >= embd_inp.size()) {
            LOG_TEE("%s: session file has exact match for prompt!\n", __func__);
        } else if (n_matching_session_tokens < (embd_inp.size() / 2)) {
            LOG_TEE("%s: warning: session file has low similarity to prompt (%zu / %zu tokens); will mostly be reevaluated\n",
                __func__, n_matching_session_tokens, embd_inp.size());
        } else {
            LOG_TEE("%s: session file matches %zu / %zu tokens of prompt\n",
                __func__, n_matching_session_tokens, embd_inp.size());
        }

        // remove any "future" tokens that we might have inherited from the previous session
        llama_kv_cache_seq_rm(ctx, -1, n_matching_session_tokens, -1);

        LOGLN(
                "recalculate the cached logits (check): embd_inp.empty() %s, n_matching_session_tokens %zu, embd_inp.size() %zu, session_tokens.size() %zu, embd_inp.size() %zu",
                log_tostr(embd_inp.empty()), n_matching_session_tokens, embd_inp.size(), session_tokens.size(), embd_inp.size());

        // if we will use the cache for the full prompt without reaching the end of the cache, force
        // reevaluation of the last token token to recalculate the cached logits
        if (!embd_inp.empty() && n_matching_session_tokens == embd_inp.size() && session_tokens.size() > embd_inp.size()) {
            LOGLN("recalculate the cached logits (do): session_tokens.resize( %zu )", embd_inp.size() - 1);

            session_tokens.resize(embd_inp.size() - 1);
        }
    }
}


void LLM::setup_context(){
    if (!params.antiprompt.empty()) {
        for (const auto & antiprompt : params.antiprompt) {
            LOG_TEE("Reverse prompt: '%s'\n", antiprompt.c_str());
            if (params.verbose_prompt) {
                auto tmp = ::llama_tokenize(ctx, antiprompt, false, true);
                for (int i = 0; i < (int) tmp.size(); i++) {
                    LOG_TEE("%6d -> '%s'\n", tmp[i], llama_token_to_piece(ctx, tmp[i]).c_str());
                }
            }
        }
    }

    if (params.input_prefix_bos) {
        LOG_TEE("Input prefix with BOS\n");
    }

    if (!params.input_prefix.empty()) {
        LOG_TEE("Input prefix: '%s'\n", params.input_prefix.c_str());
        if (params.verbose_prompt) {
            auto tmp = ::llama_tokenize(ctx, params.input_prefix, true, true);
            for (int i = 0; i < (int) tmp.size(); i++) {
                LOG_TEE("%6d -> '%s'\n", tmp[i], llama_token_to_piece(ctx, tmp[i]).c_str());
            }
        }
    }

    if (!params.input_suffix.empty()) {
        LOG_TEE("Input suffix: '%s'\n", params.input_suffix.c_str());
        if (params.verbose_prompt) {
            auto tmp = ::llama_tokenize(ctx, params.input_suffix, false, true);
            for (int i = 0; i < (int) tmp.size(); i++) {
                LOG_TEE("%6d -> '%s'\n", tmp[i], llama_token_to_piece(ctx, tmp[i]).c_str());
            }
        }
    }
}


void LLM::context_swapping(){
    // infinite text generation via context swapping
    // if we run out of context:
    // - take the n_keep first tokens from the original prompt (via n_past)
    // - take half of the last (n_ctx - n_keep) tokens and recompute the logits in batches
    if (n_past + (int) embd.size() + std::max<int>(0, guidance_offset) > n_ctx) {
        const int n_left    = n_past - params.n_keep - 1;
        const int n_discard = n_left/2;

        LOG("context full, swapping: n_past = %d, n_left = %d, n_ctx = %d, n_keep = %d, n_discard = %d\n",
            n_past, n_left, n_ctx, params.n_keep, n_discard);

        llama_kv_cache_seq_rm   (ctx, 0, params.n_keep + 1            , params.n_keep + n_discard + 1);
        llama_kv_cache_seq_shift(ctx, 0, params.n_keep + 1 + n_discard, n_past, -n_discard);

        n_past -= n_discard;

        if (ctx_guidance) {
            n_past_guidance -= n_discard;
        }

        LOG("after swap: n_past = %d, n_past_guidance = %d\n", n_past, n_past_guidance);

        LOG("embd: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, embd).c_str());

        LOG("clear session path\n");
        path_session.clear();
    }
}

void LLM::reuse_matching_prefix(){
    // try to reuse a matching prefix from the loaded session instead of re-eval (via n_past)
    if (n_session_consumed < (int) session_tokens.size()) {
        size_t i = 0;
        for ( ; i < embd.size(); i++) {
            if (embd[i] != session_tokens[n_session_consumed]) {
                session_tokens.resize(n_session_consumed);
                break;
            }

            n_past++;
            n_session_consumed++;

            if (n_session_consumed >= (int) session_tokens.size()) {
                ++i;
                break;
            }
        }
        if (i > 0) {
            embd.erase(embd.begin(), embd.begin() + i);
        }
    }
}


void LLM::shift_past_guidance(){
    // embd is typically prepared beforehand to fit within a batch, but not always
    int input_size = 0;
    llama_token * input_buf = NULL;

    if (n_past_guidance < (int) guidance_inp.size()) {
        // Guidance context should have the same data with these modifications:
        //
        // * Replace the initial prompt
        // * Shift everything by guidance_offset
        embd_guidance = guidance_inp;
        if (embd.begin() + original_prompt_len < embd.end()) {
            embd_guidance.insert(
                embd_guidance.end(),
                embd.begin() + original_prompt_len,
                embd.end()
            );
        }

        input_buf  = embd_guidance.data();
        input_size = embd_guidance.size();

        // LOG("guidance context: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, embd_guidance).c_str());
    } else {
        input_buf  = embd.data();
        input_size = embd.size();
    }

    for (int i = 0; i < input_size; i += params.n_batch) {
        int n_eval = std::min(input_size - i, params.n_batch);
        // TODO fail
        if (llama_decode(ctx_guidance, llama_batch_get_one(input_buf + i, n_eval, n_past_guidance, 0))) {
            LOG_TEE("%s : failed to eval\n", __func__);
            // return 1;
        }

        n_past_guidance += n_eval;
    }
}


void LLM::eval_tokens_in_batches(){
    for (int i = 0; i < (int) embd.size(); i += params.n_batch) {
        int n_eval = (int) embd.size() - i;
        if (n_eval > params.n_batch) {
            n_eval = params.n_batch;
        }

        LOG("eval: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, embd).c_str());

        if (llama_decode(ctx, llama_batch_get_one(&embd[i], n_eval, n_past, 0))) {
            LOG_TEE("%s : failed to eval\n", __func__);
            // TODO fail
            // return 1;
        }

        n_past += n_eval;

        LOG("n_past = %d\n", n_past);
    }
}

void LLM::push_prompt_to_sampling_context(){
    if ((int) embd_inp.size() <= n_consumed && !is_interacting) {
        // optionally save the session on first sample (for faster prompt loading next time)
        if (!path_session.empty() && need_to_save_session && !params.prompt_cache_ro) {
            need_to_save_session = false;
            llama_save_session_file(ctx, path_session.c_str(), session_tokens.data(), session_tokens.size());

            LOG("saved session to %s\n", path_session.c_str());
        }

        const llama_token id = llama_sampling_sample(ctx_sampling, ctx, ctx_guidance);

        llama_sampling_accept(ctx_sampling, ctx, id, true);

        LOG("last: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, ctx_sampling->prev).c_str());

        embd.push_back(id);

        // echo this to console
        input_echo = true;

        // decrement remaining sampling budget
        --n_remain;

        LOG("n_remain: %d\n", n_remain);
    } else {
        // some user input remains from prompt or interaction, forward it to processing
        LOG("embd_inp.size(): %d, n_consumed: %d\n", (int) embd_inp.size(), n_consumed);
        while ((int) embd_inp.size() > n_consumed) {
            embd.push_back(embd_inp[n_consumed]);

            // push the prompt in the sampling context in order to apply repetition penalties later
            // for the prompt, we don't apply grammar rules
            llama_sampling_accept(ctx_sampling, ctx, embd_inp[n_consumed], false);

            ++n_consumed;
            if ((int) embd.size() >= params.n_batch) {
                break;
            }
        }
    }
}

void LLM::display_text(){
    for (auto id : embd) {
        const std::string token_str = llama_token_to_piece(ctx, id);
        printf("%s", token_str.c_str());

        if (embd.size() > 1) {
            input_tokens.push_back(id);
        } else {
            output_tokens.push_back(id);
            output_ss << token_str;
        }
    }
    fflush(stdout);
}

bool LLM::check_reverse_prompt(){
    bool is_antiprompt = false;

    // check for reverse prompt in the last n_prev tokens
    if (!params.antiprompt.empty()) {
        const int n_prev = 32;
        const std::string last_output = llama_sampling_prev_str(ctx_sampling, ctx, n_prev);

        is_antiprompt = false;
        // Check if each of the reverse prompts appears at the end of the output.
        // If we're not running interactively, the reverse prompt might be tokenized with some following characters
        // so we'll compensate for that by widening the search window a bit.
        for (std::string & antiprompt : params.antiprompt) {
            size_t extra_padding = params.interactive ? 0 : 2;
            size_t search_start_pos = last_output.length() > static_cast<size_t>(antiprompt.length() + extra_padding)
                ? last_output.length() - static_cast<size_t>(antiprompt.length() + extra_padding)
                : 0;

            if (last_output.find(antiprompt, search_start_pos) != std::string::npos) {
                if (params.interactive) {
                    is_interacting = true;
                }
                is_antiprompt = true;
                break;
            }
        }

        if (is_antiprompt) {
            LOG("found antiprompt: %s\n", last_output.c_str());
        }
    }

    // deal with end of text token in interactive mode
    if (llama_sampling_last(ctx_sampling) == llama_token_eos(model)) {
        LOG("found EOS token\n");

        if (params.interactive) {
            if (!params.antiprompt.empty()) {
                // tokenize and inject first reverse prompt
                const auto first_antiprompt = ::llama_tokenize(ctx, params.antiprompt.front(), false, true);
                embd_inp.insert(embd_inp.end(), first_antiprompt.begin(), first_antiprompt.end());
                is_antiprompt = true;
            }

            is_interacting = true;
            printf("\n");
        } else if (params.instruct) {
            is_interacting = true;
        }
    }

    return is_antiprompt;
}


std::string LLM::get_user_input(){
    std::string buffer;
    LOG("waiting for user input\n");
    std::string line;
    bool another_line = true;
    do {
        another_line = LLMParser::readline(line);
        buffer += line;
    } while (another_line);
    return buffer;
}

void LLM::query(std::string buffer){
    if (params.instruct) {
        printf("\n> ");
    }

    if (params.input_prefix_bos) {
        LOG("adding input prefix BOS token\n");
        embd_inp.push_back(llama_token_bos(model));
    }

    if (!params.input_prefix.empty()) {
        LOG("appending input prefix: '%s'\n", params.input_prefix.c_str());
        printf("%s", params.input_prefix.c_str());
    }

    add_tokens_to_embd(buffer);
    input_echo = false; // do not echo this again
    llama_sampling_reset(ctx_sampling);
    is_interacting = false;
}


void LLM::add_tokens_to_embd(std::string& buffer){
    // Add tokens to embd only if the input buffer is non-empty
    // Entering a empty line lets the user pass control back
    if (buffer.length() > 1) {
        // append input suffix if any
        if (!params.input_suffix.empty()) {
            LOG("appending input suffix: '%s'\n", params.input_suffix.c_str());
            printf("%s", params.input_suffix.c_str());
        }

        LOG("buffer: '%s'\n", buffer.c_str());

        const size_t original_size = embd_inp.size();

        // instruct mode: insert instruction prefix
        if (params.instruct && !is_antiprompt) {
            LOG("inserting instruction prefix\n");
            n_consumed = embd_inp.size();
            embd_inp.insert(embd_inp.end(), inp_pfx.begin(), inp_pfx.end());
        }
        if (params.escape) {
            process_escapes(buffer);
        }

        const auto line_pfx = ::llama_tokenize(ctx, params.input_prefix, false, true);
        const auto line_inp = ::llama_tokenize(ctx, buffer,              false, false);
        const auto line_sfx = ::llama_tokenize(ctx, params.input_suffix, false, true);
        LOG("input tokens: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, line_inp).c_str());

        embd_inp.insert(embd_inp.end(), line_pfx.begin(), line_pfx.end());
        embd_inp.insert(embd_inp.end(), line_inp.begin(), line_inp.end());
        embd_inp.insert(embd_inp.end(), line_sfx.begin(), line_sfx.end());

        // instruct mode: insert response suffix
        if (params.instruct) {
            LOG("inserting instruction suffix\n");
            embd_inp.insert(embd_inp.end(), inp_sfx.begin(), inp_sfx.end());
        }

        for (size_t i = original_size; i < embd_inp.size(); ++i) {
            const llama_token token = embd_inp[i];
            output_tokens.push_back(token);
            output_ss << llama_token_to_piece(ctx, token);
        }

        n_remain -= line_inp.size();
        LOG("n_remain: %d\n", n_remain);
    } else {
        LOG("empty line, passing control back\n");
    }
}

LLM::~LLM(){
    if (!path_session.empty() && params.prompt_cache_all && !params.prompt_cache_ro) {
        LOG_TEE("\n%s: saving final output to session file '%s'\n", __func__, path_session.c_str());
        llama_save_session_file(ctx, path_session.c_str(), session_tokens.data(), session_tokens.size());
    }

    llama_print_timings(ctx);
    // write_logfile(ctx, params, model, input_tokens, output_ss.str(), output_tokens);

    if (ctx_guidance) { llama_free(ctx_guidance); }
    llama_free(ctx);
    llama_free_model(model);

    llama_sampling_free(ctx_sampling);
    llama_backend_free();

    LOG_TEE("Log end\n");
}


void LLM::init(){
    LOG("%s: llama backend init\n", __func__);
    llama_backend_init(params.numa);

    ctx_guidance = NULL;
    guidance_offset = 0;
    original_prompt_len = 0;
    path_session = params.path_prompt_cache;

    load_model();
    set_context_num();
    load_saved_session();

    add_bos = llama_vocab_type(model) == LLAMA_VOCAB_TYPE_SPM;
    LOG("add_bos: %d\n", add_bos);
    init_embedding_input();
    if (params.n_keep < 0 || params.n_keep > (int) embd_inp.size() || params.instruct) {
        params.n_keep = (int)embd_inp.size();
    }
    tokenize_negative_prompt();

    session_similarity();

    inp_pfx = ::llama_tokenize(ctx, "\n\n### Instruction:\n\n", add_bos, true);
    inp_sfx = ::llama_tokenize(ctx, "\n\n### Response:\n\n",    false,   true);
    LOG("inp_pfx: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, inp_pfx).c_str());
    LOG("inp_sfx: %s\n", LOG_TOKENS_TOSTR_PRETTY(ctx, inp_sfx).c_str());
    setup_context();

    ctx_sampling = llama_sampling_init(params.sparams);
    LOG_TEE("sampling: \n%s\n", llama_sampling_print(params.sparams).c_str());
    LOG_TEE("generate: n_ctx = %d, n_batch = %d, n_predict = %d, n_keep = %d\n", n_ctx, params.n_batch, params.n_predict, params.n_keep);
    LOG_TEE("\n\n");

    reset();
}


void LLM::reset(){
    is_interacting = params.interactive_first;

    is_antiprompt        = false;
    input_echo           = true;
    need_to_save_session = !path_session.empty() && n_matching_session_tokens < embd_inp.size();

    n_past             = 0;
    n_remain           = params.n_predict;
    n_consumed         = 0;
    n_session_consumed = 0;
    n_past_guidance    = 0;

    input_tokens.clear();
    output_tokens.clear();
    output_ss.clear();
    embd.clear();
    embd_guidance.clear();
}

void LLM::answer(){
    do {
        // predict
        if (!embd.empty()) {
            // Note: n_ctx - 4 here is to match the logic for commandline prompt handling via
            // --prompt or --file which uses the same value.
            int max_embd_size = n_ctx - 4;

            // Ensure the input doesn't exceed the context size by truncating embd if necessary.
            if ((int) embd.size() > max_embd_size) {
                const int skipped_tokens = (int) embd.size() - max_embd_size;
                embd.resize(max_embd_size);
                // TODO warning
                printf("<<input too long: skipped %d token%s>>", skipped_tokens, skipped_tokens != 1 ? "s" : "");
                fflush(stdout);
            }

            // infinite text generation via context swapping
            if (n_past + (int) embd.size() + std::max<int>(0, guidance_offset) > n_ctx && params.n_predict == -2) {
                LOG_TEE("\n\n%s: context full and n_predict == -%d => stopping\n", __func__, params.n_predict);
                break;
            }
            context_swapping();
            reuse_matching_prefix();

            // evaluate tokens in batches
            // embd is typically prepared beforehand to fit within a batch, but not always
            if (ctx_guidance) 
                shift_past_guidance();
            
            eval_tokens_in_batches();

            if (!embd.empty() && !path_session.empty()) {
                session_tokens.insert(session_tokens.end(), embd.begin(), embd.end());
                n_session_consumed = session_tokens.size();
            }
        }

        embd.clear();
        embd_guidance.clear();

        push_prompt_to_sampling_context();

        // display text
        if (input_echo) display_text();

        // In interactive mode, respect the maximum number of tokens and drop back to user input when reached.
        // We skip this logic when n_predict == -1 (infinite) or -2 (stop at context size).
        if (n_remain <= 0 && params.n_predict >= 0) {
            n_remain = params.n_predict;
            is_interacting = true;
        }

        // if not currently processing queued inputs;
        if ((int) embd_inp.size() <= n_consumed) {
            is_antiprompt = check_reverse_prompt();
        }
    } while(!is_interacting);
}



void LLM::run(){
    while (true) {
        answer();
        query(get_user_input());
    }
}


int main(int argc, char ** argv) {
    gpt_params params;
    if (argc <= 1){
        std::string params_string =  R"(-i -m /home/benuix/codes/llama.cpp/mistral-7b-v0.1.Q4_K_M.gguf -ngl 32 -c 4096 --keep 256 --repeat_penalty 1.1 --prompt "Transcript of a dialog, where the User interacts with an Assistant named Lucy. Lucy is a friendly dinosaur." -r "User:" -s 1234)";
        params = LLMParser::string_to_gpt_params(params_string);
    }else {
        if (!gpt_params_parse(argc, argv, params)) {
            return 1;
        }
    }
    LLM llm(params);
    llm.run();
    return 0;
}
