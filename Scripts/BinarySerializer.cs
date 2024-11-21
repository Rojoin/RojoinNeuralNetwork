using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

internal static class BinarySerializer
{
    public static void SerializeToFile<T>(T obj, string filePath)
    {
        try
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Serialization error: {ex.Message}");
        }
    }

    public static T DeserializeFromFile<T>(string filePath) where T : class
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                IFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(stream) as T;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization error: {ex.Message}");
            return null;
        }
    }

    public static void SerializeCollectionToFile<T>(IEnumerable<T> objects, string filePath)
    {
        try
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, objects.ToList());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Serialization error: {ex.Message}");
        }
    }

    public static List<T> DeserializeCollectionFromFile<T>(string filePath) where T : class
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                IFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(stream) as List<T>;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization error: {ex.Message}");
            return null;
        }
    }
}

public static class GeneticAlgorithmDataBatchHandler
{
    private const string FileName = "GeneticAlgorithmDataBatch.bin";

    public static void SaveBatch(IEnumerable<GeneticAlgorithmData> dataList, string filePath)
    {
        BinarySerializer.SerializeCollectionToFile(dataList, filePath);
    }

    public static List<GeneticAlgorithmData> LoadBatch(string filePath)
    {
        return BinarySerializer.DeserializeCollectionFromFile<GeneticAlgorithmData>(filePath);
    }
}