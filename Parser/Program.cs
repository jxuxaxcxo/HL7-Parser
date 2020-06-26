using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Parser
{
    public class HL7_Instance
    {
        [JsonProperty("messageTypes")]
        public List<HL7_MessageType> messageTypes;

        [JsonProperty("globalFields")]
        public List<HL7_Field> globalFields;
    }

    public class HL7_MessageType
    {
        [JsonProperty("title")]
        public string title;

        [JsonProperty("segmentList")]
        public List<HL7_Segment> segmentList;
    }

    public class HL7_Segment
    {
        [JsonProperty("segmentTitle")]
        public string segmentTitle;

        [JsonProperty("segmentOptionalityType")]
        public string segmentOptionalityType;
        
        [JsonProperty("segmentRepeteability")]
        public string segmentRepeteability;
    }

    public class HL7_Field
    {
        [JsonProperty("title")]
        public string title { get; set; }

        [JsonProperty("fields")]
        public List<HL7_Field_Parts> fieldParts { get; set; }
    }

    public class HL7_Field_Parts
    {
        [JsonProperty("fieldTitle")]
        public String fieldTitle { get; set; }

        [JsonProperty("dataType")]
        public String dataType { get; set; }

        [JsonProperty("fieldOptionality")]
        public String fieldOptionality { get; set; }

        [JsonProperty("fieldRepeteability")]
        public String fieldRepeteability { get; set; }

        [JsonProperty("fieldTable")]
        public String fieldTable { get; set; }
    }
    class Program
    {
        public String textToAnalize;
        public static HL7_Instance HL7Instance;
        public static List<HL7_Field> segments;
        
        public static List<String> receivedMessageSegments;
        static void Main(string[] args)
        {
            initialize();
            readText();
        }

        public static void initialize()
        {
            segments = new List<HL7_Field>();
            receivedMessageSegments = new List<String>();
            loadJSON();
        }

        public static void loadJSON()
        {
            string path = @"C:\Users\VARGAS\source\repos\Parser\Parser\HL7_2_3.json";
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                HL7Instance = JsonConvert.DeserializeObject<HL7_Instance>(json);
                segments = HL7Instance.globalFields;
            }
        }

        public static void readText()
        {
            FileStream fileStream = new FileStream(@"C:\Users\VARGAS\Desktop\SampleHL7.txt", FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8);
            {
                string line, firstLine = "";
                bool isFirstLine = true;

                while ((line = streamReader.ReadLine()) != null)
                {
                    if (isFirstLine)
                    {
                        firstLine = line;
                        isFirstLine = false;
                    }
                    analizeText(line);
                }
                getMessageInfo(firstLine);
            }
        }

        public static void getMessageInfo(string line)
        {
            String[] mshSections = line.Split('|');
            String messageType = mshSections[8];

            checkMessageSintaxStructure(messageType);
        }

        public static void checkMessageSintaxStructure(String messageType)
        {
            Console.WriteLine("Checking Sintax for: " + messageType);
            List<HL7_Segment> messageTypeStructure = HL7Instance.messageTypes[getMessageTypeIndex(messageType)].segmentList;

            for (int i = 0; i < messageTypeStructure.Count; i++)
            {
                if (messageTypeStructure[i].segmentOptionalityType == "R" && 
                    !receivedMessageSegments.Contains(messageTypeStructure[i].segmentTitle))
                {                    
                    Console.WriteLine("---ERROR---");
                    Console.WriteLine("El mensaje enviado no tiene el Segmento Obligatorio: " + messageTypeStructure[i].segmentTitle);
                    return;
                }

                if (messageTypeStructure[i].segmentRepeteability == "N" &&
                   receivedMessageSegments.FindAll(x => x == messageTypeStructure[i].segmentTitle).Count > 1)
                {
                    Console.WriteLine("---ERROR---");
                    Console.WriteLine("El segmento " + messageTypeStructure[i].segmentTitle+ " no puede ser repetido en este tipo de mensaje");
                    return;
                }
                        
            }
            Console.WriteLine("El mensaje tiene los segmentos necesarios");
            Console.WriteLine("El mensaje no repite los segmentos que no debe");
            Console.WriteLine("El mensaje no tiene segmentos extra");

            int structureIndex = 0;
            int messageIndex = 0;

            while (structureIndex < messageTypeStructure.Count)
            {
                if (messageTypeStructure[structureIndex].segmentTitle == receivedMessageSegments[messageIndex])
                {
                    structureIndex++;
                    messageIndex++;
                    while (messageIndex < receivedMessageSegments.Count -1)
                    {
                        if (receivedMessageSegments[messageIndex] != receivedMessageSegments[messageIndex + 1])
                            break;
                        else
                            messageIndex++;
                    }
                }
                else
                    structureIndex++;
            }

            if (messageIndex != receivedMessageSegments.Count - 1)
            {
                Console.WriteLine("---ERROR---");
                Console.WriteLine("El orden de los segmentos del mensaje: " + messageType + " es INCORRECTO o existe un segmento que NO PERTENCE a esta estructura");
                return;
            }
            else
                Console.WriteLine("El orden de los segmentos es correcto");      
        }

        public static void checkFieldsValues(String fieldTitle, List<HL7_Field_Parts> fields, String[] fieldSections)
        {
            for(int i = 0; i < fieldSections.Length-1; i++)
            {
            
                if(fields[i].fieldOptionality == "R" && fieldSections[i+1] == "")
                {
                    Console.WriteLine("El segmento \"" + fields[i].fieldTitle+"\" es REQUERIDO y esta vacio");
                    break;
                }
                
                if (fieldSections[i + 1].Trim() != "")
                    Console.WriteLine(fieldTitle + "-" + (i+1)+ ":"+fields[i].fieldTitle+":"+ fieldSections[i+1]);
              
            }

        }
        public static int getMessageTypeIndex(String messageType)
        {
            for(int i = 0; i<HL7Instance.messageTypes.Count; i++)
            {
                if (HL7Instance.messageTypes[i].title == messageType)
                    return i;
            }
            return -1;
        }

        public static void analizeText(String text)
        {
            String[] sections = text.Split('|');

            String output = "";
            int tagIndex = getTagIndex(sections[0]);

            checkFieldsValues(segments[tagIndex].title, segments[tagIndex].fieldParts, sections);

            receivedMessageSegments.Add(segments[tagIndex].title);
            Console.WriteLine(output);
        }

        public static int getTagIndex(String tag)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].title == tag)
                    return i;
            }
            return -1;
        }
    }
}
