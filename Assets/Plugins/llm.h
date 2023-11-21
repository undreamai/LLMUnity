#pragma once
#include "common.h"
#include "llama.h"

void check_params(gpt_params params);

void load_model(
    gpt_params params,
    llama_model *& model,
    llama_context *& ctx,
    llama_context *& ctx_guidance
);

int get_context_num(
    llama_model * model,
    llama_context * ctx
);

void load_saved_session(
    gpt_params params,
    std::string path_session,
    std::vector<llama_token>& session_tokens,
    llama_context *& ctx,
    const int n_ctx
);

std::vector<llama_token> init_embedding_input(
    gpt_params params,
    std::vector<llama_token> session_tokens,
    llama_context * ctx,
    const int n_ctx,
    llama_model * model,
    const bool add_bos
);

void tokenize_negative_prompt(
    gpt_params params,
    llama_context * ctx_guidance,
    llama_context * ctx,
    std::vector<llama_token>& guidance_inp,
    const bool add_bos,
    int& guidance_offset,
    int& original_prompt_len
);

void session_similarity(
    gpt_params params,
    std::vector<llama_token> session_tokens,
    llama_context *& ctx,
    std::vector<llama_token> embd_inp,
    size_t& n_matching_session_tokens
);

void setup_context(gpt_params params, llama_context ** ctx);

void context_swapping(
    gpt_params params,
    std::vector<llama_token> embd,
    llama_context *& ctx,
    int guidance_offset,
    llama_context * ctx_guidance,
    const int n_ctx,
    int& n_past,
    int& n_past_guidance,
    std::string& path_session
);

void reuse_matching_prefix(
    std::vector<llama_token>& embd,
    std::vector<llama_token>& session_tokens,
    int& n_session_consumed,
    int& n_past
);

void shift_past_guidance(
    gpt_params params,
    // llama_context * ctx,
    llama_context*& ctx_guidance,
    int& n_past_guidance,
    std::vector<llama_token>& guidance_inp,
    std::vector<llama_token>& embd_guidance,
    std::vector<llama_token>& embd,
    int original_prompt_len
);

void eval_tokens_in_batches(
    gpt_params params,
    std::vector<llama_token>& embd,
    llama_context *& ctx,
    int& n_past
);

void push_prompt_to_sampling_context(
    gpt_params params,
    std::vector<llama_token>& embd,
    std::vector<llama_token> embd_inp,
    llama_context *& ctx,
    bool need_to_save_session,
    std::string path_session,
    std::vector<llama_token> session_tokens,
    int& n_consumed,
    int& n_remain,
    bool& input_echo,
    struct llama_sampling_context *& ctx_sampling,
    llama_context *& ctx_guidance,
    bool& is_interacting
);

void display_text(
    std::vector<llama_token> embd,
    llama_context * ctx,
    std::vector<int>& input_tokens,
    std::vector<int>& output_tokens,
    std::ostringstream& output_ss
);

bool check_reverse_prompt(
    gpt_params params,
    llama_context * ctx,
    struct llama_sampling_context * ctx_sampling,
    std::vector<llama_token>& embd_inp,
    llama_model * model,
    bool& is_interacting
);

static bool readline(std::string & line);

std::string get_user_input(
    gpt_params params,
    std::vector<llama_token>& embd_inp,
    llama_model * model
);

void add_tokens_to_embd(
    gpt_params params,
    llama_context * ctx,
    std::string& buffer,
    std::vector<llama_token>& embd_inp,
    bool is_antiprompt,
    int& n_consumed,
    const std::vector<llama_token> inp_pfx, 
    const std::vector<llama_token> inp_sfx,
    std::vector<int>& output_tokens,
    std::ostringstream& output_ss,
    int& n_remain
);

void finalise(
    gpt_params params,
    std::string path_session,
    llama_context *& ctx,
    llama_context *& ctx_guidance,
    std::vector<llama_token>& session_tokens,
    llama_model *& model,
    struct llama_sampling_context *& ctx_sampling
);

void run(
    gpt_params params,
    std::string path_session,
    size_t n_matching_session_tokens,
    std::vector<llama_token> embd_inp,
    int n_ctx,
    std::vector<llama_token> guidance_inp,
    int guidance_offset,
    int original_prompt_len,
    llama_context * ctx,
    llama_context * ctx_guidance,
    struct llama_sampling_context * ctx_sampling,
    std::vector<llama_token> session_tokens,
    const std::vector<llama_token> inp_pfx, 
    const std::vector<llama_token> inp_sfx,
    llama_model * model
);