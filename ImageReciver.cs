using UnityEngine;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using LLMUnity;
using LLMUnitySamples;

public class ImageReciver : MonoBehaviour
{
    //Noskop_dev was here :D

    //This field is used to relay the image to the AI, this can be done by both a URL or a file in your system.
    public string AnyImageData;

    // Should work with any script that calls the Chat function on the LLMCharacter script. 
    public AndroidDemo AD;
  
  

    void Start()
    {
      
       
      
        
    }

   
    
    public void SendImageToAI()  
    {
        AD.onInputFieldSubmit(" [\r\n        {\"role\": \"system\", \"content\": \"You are an assistant who perfectly describes images.\"},\r\n        {\r\n            \"role\": \"user\",\r\n            \"content\": [\r\n                {\"type\" : \"text\", \"text\": \"What's in this image?\"},\r\n                {\"type\": \"image_url\", \"image_url\": {\"url\":" + AnyImageData + "\" } }\r\n            ]");

       
    }
}
