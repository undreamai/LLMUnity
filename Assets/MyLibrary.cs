using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;

public class MyLibrary : MonoBehaviour
{
    void Start()
    {
        MyStringClassWrapper MyStringClass = new MyStringClassWrapper();
        string result = MyStringClass.GetString();
        Debug.Log("Result from DLL: " + result);
    }
}
