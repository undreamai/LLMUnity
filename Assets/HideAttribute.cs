using UnityEngine;
using System;
 
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public class HideAttribute : PropertyAttribute
{
    public string Header = "";
    //The name of the bool field that will be in control
    public string ConditionalSourceField = "";
    //TRUE = Hide in inspector / FALSE = Disable in inspector 
    public bool HideInInspector = false;
 
    public HideAttribute(string conditionalSourceField, string Header="", bool hideInInspector=true)
    {
        this.ConditionalSourceField = conditionalSourceField;
        this.HideInInspector = hideInInspector;
        this.Header = Header;
    }
}