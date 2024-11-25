using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using RojoinSaveSystem.Attributes;

namespace RojoinNeuralNetwork.Save
{
    public static class SaveSystem
    {
        // public static Dictionary<string, object?> SerializeObject(object obj)
        // {
        //     var result = new Dictionary<string, object?>();
        //     var type = obj.GetType();
        //
        //     foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        //     {
        //         var attribute = field.GetCustomAttribute<SaveValue>();
        //         if (attribute == null)
        //             continue;
        //
        //         object fieldValue = field.GetValue(obj);
        //         if (fieldValue == null)
        //         {
        //             result[attribute.id.ToString()] = null;
        //             continue;
        //         }
        //
        //         var fieldType = field.FieldType;
        //
        //         if (fieldType.IsArray)
        //         {
        //             // Handle arrays
        //             Array array = (Array)fieldValue;
        //             List<object> serializedArray = new List<object>();
        //
        //             foreach (object element in array)
        //             {
        //                 serializedArray.Add(element != null && !fieldType.GetElementType()!.IsPrimitive
        //                     ? SerializeObject(element)
        //                     : element);
        //             }
        //
        //             result[attribute.id.ToString()] = serializedArray;
        //         }
        //         else if (typeof(IEnumerable).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
        //         {
        //             IEnumerable list = (IEnumerable)fieldValue;
        //             var serializedList = new List<object?>();
        //
        //             foreach (object element in list)
        //             {
        //                 serializedList.Add(element != null && !element.GetType().IsPrimitive
        //                     ? SerializeObject(element)
        //                     : element);
        //             }
        //
        //             result[attribute.id.ToString()] = serializedList;
        //         }
        //         else if (!fieldType.IsPrimitive && !fieldType.IsValueType && fieldType != typeof(string))
        //         {
        //             // Handle nested objects
        //             result[attribute.id.ToString()] = SerializeObject(fieldValue);
        //         }
        //         else
        //         {
        //             // Handle primitives and strings
        //             result[attribute.id.ToString()] = fieldValue;
        //         }
        //     }
        //
        //     return result;
        // }
        //
        // public static void DeserializeObject(object obj, Dictionary<string, object?> data)
        // {
        //     Type type = obj.GetType();
        //
        //     foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
        //                                                BindingFlags.Instance))
        //     {
        //         var attribute = field.GetCustomAttribute<SaveValue>();
        //         if (attribute == null)
        //             continue;
        //
        //         string key = attribute.id.ToString();
        //         if (!data.ContainsKey(key))
        //             continue;
        //
        //         var fieldType = field.FieldType;
        //         object value = data[key];
        //
        //         if (value == null)
        //         {
        //             field.SetValue(obj, null);
        //             continue;
        //         }
        //
        //         try
        //         {
        //             if (fieldType.IsArray)
        //             {
        //                 var elementType = fieldType.GetElementType();
        //                 if (value is IList arrayData) 
        //                 {
        //                     var array = Array.CreateInstance(elementType!, arrayData.Count);
        //
        //                     for (int i = 0; i < arrayData.Count; i++)
        //                     {
        //                         if (arrayData[i] is Dictionary<string, object> nestedData)
        //                         {
        //                             object elementInstance = Activator.CreateInstance(elementType);
        //                             DeserializeObject(elementInstance, nestedData);
        //                             array.SetValue(elementInstance, i);
        //                         }
        //                         else
        //                         {
        //                             array.SetValue(Convert.ChangeType(arrayData[i], elementType), i);
        //                         }
        //                     }
        //
        //                     field.SetValue(obj, array);
        //                 }
        //             }
        //             else if (typeof(IEnumerable).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
        //             {
        //                 // Handle lists
        //                 var listType = fieldType.GetGenericArguments()[0];
        //                 var listData = value as List<object>;
        //                 if (listData == null) continue;
        //
        //                 var listInstance = (IList)Activator.CreateInstance(fieldType);
        //                 foreach (object element in listData)
        //                 {
        //                     if (element is Dictionary<string, object?> nestedData)
        //                     {
        //                         object elementInstance = Activator.CreateInstance(listType);
        //                         DeserializeObject(elementInstance, nestedData);
        //                         listInstance.Add(elementInstance);
        //                     }
        //                     else
        //                     {
        //                         listInstance.Add(Convert.ChangeType(element, listType));
        //                     }
        //                 }
        //
        //                 field.SetValue(obj, listInstance);
        //             }
        //             else if (!fieldType.IsPrimitive && !fieldType.IsValueType && fieldType != typeof(string))
        //             {
        //                 object nestedObject = Activator.CreateInstance(fieldType);
        //                 DeserializeObject(nestedObject, (Dictionary<string, object?>)value);
        //                 field.SetValue(obj, nestedObject);
        //             }
        //             else
        //             {
        //                 field.SetValue(obj, Convert.ChangeType(value, fieldType));
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             throw new Exception(
        //                 $"Error deserializing field '{field.Name}' of type '{fieldType}': {ex.Message}");
        //         }
        //     }
        // }


        public class GeneticAlgorithmDataManager
        {
            private List<GeneticAlgorithmData> _datasets = new List<GeneticAlgorithmData>();
            
            public void AddDataset(GeneticAlgorithmData data)
            {
                _datasets.Add(data);
            }

            public void SaveAll(string filePath)
            {
                MemoryStream stream = new MemoryStream();
                foreach (GeneticAlgorithmData data in _datasets)
                {
                    byte[] dataArray = data.Serialize();
                    stream.Capacity += dataArray.Length;
                    stream.Write(dataArray, 0, dataArray.Length);
                }

                File.WriteAllBytes(filePath, stream.ToArray());
            }

            public void LoadAll(string filePath)
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Save file not found.");

                byte[] data = File.ReadAllBytes(filePath);

                int offset = 0;
                for (int index = 0; index < _datasets.Count; index++)
                {
                    _datasets[index] = new GeneticAlgorithmData(data, ref offset);
                }

            }

            public List<GeneticAlgorithmData> GetAllDatasets()
            {
                return _datasets;
            }  
            public void ClearDatasets()
            {
               _datasets.Clear();
            }
        }
    }
}