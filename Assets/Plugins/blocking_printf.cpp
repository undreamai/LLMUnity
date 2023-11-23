#include <cstdio>
#include <fstream>
#include <iostream>
#include <future>
#include <chrono>
#include <thread>
#include <unistd.h>

class PrintfStreamBlocker{
    public:
        PrintfStreamBlocker(){block();}
        ~PrintfStreamBlocker(){restore();}
        FILE* pipeStream;
        int original_stdout;

    private:
        void block(){
            // Create a pipe to capture the output
            int pipefd[2];
            if (pipe(pipefd) == -1) 
                throw std::runtime_error("pipe creation failed");

            // Save the original stdout
            original_stdout = dup(fileno(stdout));

            // Redirect stdout to the write end of the pipe
            if (dup2(pipefd[1], fileno(stdout)) == -1)
                throw std::runtime_error("dup2 redirection failed");

            // Close the write end of the pipe (it's now associated with stdout)
            close(pipefd[1]);

            // Create a C++ stream from the read end of the pipe
            pipeStream = fdopen(pipefd[0], "r");
            if (pipeStream == nullptr)
                throw std::runtime_error("read stream creation failed");
        }

        void restore(){
            if (dup2(original_stdout, fileno(stdout)) == -1) 
                throw std::runtime_error("dup2 restore failed");
            fclose(pipeStream);
            printf("restored!\n"); fflush(stdout);
        }
};


void append_to_file(std::string message, std::string filename="test.txt"){
    std::ofstream outfile;
    outfile.open(filename, std::ios_base::app);
    outfile << message;
    outfile.close();
}

bool stopThread = false;
// Function to be executed asynchronously
void myThreadFunction(FILE* pipeStream) {
    append_to_file("This is a parallel thread.\n");
    char buffer[100];
    while (!stopThread && fgets(buffer, sizeof(buffer), pipeStream) != nullptr) {
        append_to_file(std::string(buffer));
    }
    append_to_file("Exited parallel thread.\n");
}

int main() {
    PrintfStreamBlocker printfStreamBlocker;
    FILE* pipeStream = printfStreamBlocker.pipeStream;

    printf("Hello, printf!\n"); fflush(stdout);

    auto callable = [pipeStream]() { myThreadFunction(pipeStream); };
    std::future<void> result = std::async(std::launch::async, callable);
    
    for (int i = 0; i < 5; i++) {
        std::this_thread::sleep_for(std::chrono::seconds(1));
        printf("Hello, printf %d!\n", i); fflush(stdout);
    }
    printf("finito\n"); fflush(stdout);

    stopThread = true;
    return 0;
}
