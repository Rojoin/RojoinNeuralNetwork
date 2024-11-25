using System;
using System.Collections.Generic;
using RojoinSaveSystem.Attributes;

[Serializable]
public class Genome
{
    [SaveValue(0)] public float[] genome;
    public float fitness = 0;

    public Genome(float[] genes)
    {
        this.genome = genes;
        fitness = 0;
    } 
    public Genome(byte[] data,ref int output)
    {
        int genomeLength = BitConverter.ToInt32(data, output);
        output += sizeof(int);
        
        genome = new float[genomeLength];
        for (int i = 0; i < genomeLength; i++)
        {
            genome[i] = BitConverter.ToSingle(data, output);
            output += sizeof(float);
        }
        
        fitness = BitConverter.ToSingle(data, output);
        output += sizeof(float);
        
    }
    public byte[] Serialize()
    {
        List<byte> bytes = new List<byte>();

        bytes.AddRange(BitConverter.GetBytes(genome.Length));

        foreach (float gene in genome)
        {
            bytes.AddRange(BitConverter.GetBytes(gene));
        }
        
        bytes.AddRange(BitConverter.GetBytes(fitness));

        return bytes.ToArray();
    }
    public Genome(int genesCount)
    {
        genome = new float[genesCount];
        Random rand = new Random();
        for (int j = 0; j < genesCount; j++)
            genome[j] = (float)(rand.NextDouble() * 2 - 1);

        fitness = 0;
    }

    public Genome()
    {
        fitness = 0;
    }
}