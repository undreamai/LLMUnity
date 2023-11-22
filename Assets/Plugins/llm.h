#pragma once
#include "common.h"
#include "llama.h"

class LLMParser {
    public:
        static bool readline(std::string & line);
        static std::vector<std::string> splitArguments(const std::string& inputString);
        static gpt_params string_to_gpt_params(std::string params_string);
};

class LLM {
    public:
        LLM(gpt_params params);
        LLM(std::string params_string);
        ~LLM();

        void run();
        void reset();
        std::string get_user_input();
        void query(std::string buffer);
        void answer();

    private:
        gpt_params params;
        llama_model * model;
        std::vector<llama_token> session_tokens;
        std::vector<llama_token> embd_inp;
        std::vector<llama_token> guidance_inp;
        llama_context * ctx;
        int n_ctx;
        int guidance_offset;
        int original_prompt_len;
        bool add_bos;
        struct llama_sampling_context * ctx_sampling;
        llama_context * ctx_guidance;
        std::string path_session;
        size_t n_matching_session_tokens;
        std::vector<llama_token> inp_pfx;
        std::vector<llama_token> inp_sfx;

        // for run loop
        bool is_interacting;
        bool is_antiprompt;
        bool input_echo;
        bool need_to_save_session;
        int n_past;
        int n_remain;
        int n_consumed;
        int n_session_consumed;
        int n_past_guidance;
        std::vector<int>   input_tokens;
        std::vector<int>   output_tokens;
        std::ostringstream output_ss;
        std::vector<llama_token> embd;
        std::vector<llama_token> embd_guidance;

        void check_params();

        void init();
        void load_model();
        void set_context_num();
        void load_saved_session();
        void init_embedding_input();
        void tokenize_negative_prompt();
        void session_similarity();
        void setup_context();

        void context_swapping();
        void reuse_matching_prefix();
        void shift_past_guidance();
        void eval_tokens_in_batches();
        void push_prompt_to_sampling_context();
        void display_text();
        bool check_reverse_prompt();
        void add_tokens_to_embd(std::string& buffer);
};

#ifdef _WIN32
    #ifdef MYLIBRARY_EXPORTS
    #define MYLIBRARY_API __declspec(dllexport)
    #else
    #define MYLIBRARY_API __declspec(dllimport)
    #endif
#else
    #define MYLIBRARY_API
#endif

extern "C" {
	MYLIBRARY_API LLM* LLM_Create(char* params_string) { return new LLM(std::string(params_string)); }
	MYLIBRARY_API void LLM_Delete(LLM* object) { delete object; }
	MYLIBRARY_API void LLM_Query(LLM* object, char* buffer) { return object->query(std::string(buffer));}
	MYLIBRARY_API void LLM_Answer(LLM* object) { return object->answer(); }
}
