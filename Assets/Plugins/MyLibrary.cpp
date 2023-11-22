#include "MyLibrary.h"
#include <cstring>

namespace {
    const char* helloString = "Hello from DLL!";
}

const char* MyStringClass::GetStringContent() {
    return helloString;
}

int MyStringClass::GetStringSize() {
    return strlen(GetStringContent()) + 1;  // Include null terminator
}

void MyStringClass::GetString(char* buffer, int bufferSize) {
    strncpy(buffer, GetStringContent(), bufferSize);
    buffer[bufferSize - 1] = '\0';  // Ensure null-terminated string
}
