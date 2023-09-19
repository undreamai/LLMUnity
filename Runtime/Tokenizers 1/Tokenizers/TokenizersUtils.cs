using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace TokenizersUtils
{
    public static class Utils
    {
        public static List<int> Fuse(List<int> arr, int value)
        {
            List<int> fused = new List<int>();
            int i = 0;
            while (i < arr.Count)
            {
                fused.Add(arr[i]);
                if (arr[i] != value)
                {
                    i++;
                    continue;
                }

                while (i < arr.Count && arr[i] == value)
                {
                    i++;
                }
            }

            return fused;
        }
    }
}