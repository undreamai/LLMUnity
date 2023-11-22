#pragma once

#ifdef _WIN32
    #ifdef MYLIBRARY_EXPORTS
    #define MYLIBRARY_API __declspec(dllexport)
    #else
    #define MYLIBRARY_API __declspec(dllimport)
    #endif
#else
    #define MYLIBRARY_API
#endif

class MyStringClass {
    private:
        const char* GetStringContent();

    public:
        int GetStringSize();  // Function to get the required buffer size
        void GetString(char* buffer, int bufferSize);  // Actual function to get the string
};


extern "C" {
	MYLIBRARY_API MyStringClass* MyStringClass_Create() { return new MyStringClass(); }
	MYLIBRARY_API void MyStringClass_Delete(MyStringClass* object) { delete object; }
	MYLIBRARY_API int MyStringClass_GetStringSize(MyStringClass* object) { return object->GetStringSize(); }
	MYLIBRARY_API void MyStringClass_GetString(MyStringClass* object, char* buffer, int bufferSize) { return object->GetString(buffer, bufferSize); }
}