using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RegAutomation
{
    public class Pattern_Property : Pattern
    {
        public static void Process(KeyValuePair<string, DB.Type> type)
        {
            MatchCollection matches = FindMatches(type.Value.Content, "REG_PROPERTY");
            if (matches == null)
                return;

            foreach (Match match in matches)
            {
                Console.WriteLine("REG_PROPERTY: " + Path.GetFileName(type.Key));
                int startIndex = match.Value.Length + match.Index + 2;
                string sub = type.Value.Content.Substring(startIndex, type.Value.Content.Length - startIndex);
                string content = sub.Substring(0, sub.IndexOf(';')).Trim();
                var declaration = content.Split(' ');
                if (declaration.Length < 2)
                {
                    Console.WriteLine("Incorrect declaration: " + declaration);
                    continue;
                }
                string var = declaration[0];
                string name = declaration[1];
                type.Value.Properties[name] = new DB.Prop()
                {
                    Type = var
                };
            }
        }

        public static void Generate(KeyValuePair<string, DB.Type> type, ref string content, ref string inject)
        {
            string propertyBindings = "";
            string functionBindings = "";
            
            foreach (var func in type.Value.Properties)
            {
                string variant = "";
                switch (func.Value.Type)
                {
                    case "float":
                        variant = "FLOAT";
                        break;
                    default:
                        Console.WriteLine("Unknown type: " + func.Value.Type);
                        continue;
                }

                // Property bindings
                propertyBindings += "ClassDB::add_property(\"" + type.Value.Name + "\", ";
                propertyBindings += "PropertyInfo(Variant::" + variant + ", ";
                propertyBindings += "\"" + func.Key + "\"";
                // TODO: Meta!
                // ClassDB::add_property("GDExample", PropertyInfo(Variant::FLOAT, "speed", PROPERTY_HINT_RANGE, "0,20,0.01"), "set_speed", "get_speed");
                propertyBindings += "), ";
                propertyBindings += "\"set_" + func.Key + "\", ";
                propertyBindings += "\"get_" + func.Key + "\");\n\t\t";

                // Function generation
                inject += "\t" + func.Value.Type + " get_" + func.Key + "() const { return " + func.Key + "; }\n";
                inject += "\tvoid set_" + func.Key + "(" + func.Value.Type + " p) { " + func.Key + " = p; }\n";
                
                // Function bindings
                functionBindings += "ClassDB::bind_method(D_METHOD(\"get_" + func.Key + "\"), ";
                functionBindings += "&" + type.Value.Name + "::get_" + func.Key + ");\n\t";
                functionBindings += "ClassDB::bind_method(D_METHOD(\"set_" + func.Key + "\", \"p\"), ";
                functionBindings += "&" + type.Value.Name + "::set_" + func.Key + ");\n\t"; 
            }

            content = content.Replace("REG_BIND_PROPERTIES", propertyBindings);
            content = content.Replace("REG_BIND_PROPERTY_FUNCTIONS", functionBindings);
        }
    }
}