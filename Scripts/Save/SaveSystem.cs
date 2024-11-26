using System.Collections.Generic;
using System.IO;

namespace RojoinNeuralNetwork.Save
{
    public static class SaveSystem
    {
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