using System;
using System.Collections;
using System.Collections.Generic;
using RojoinSaveSystem.Attributes;


[Serializable]
public class Neuron
{
    [SaveValue(0)] public float[] weights;
    [SaveValue(1)] private float bias;
    [SaveValue(2)] private float p;

    private int offsetCalculator;
    public int WeightsCount
    {
        get { return weights.Length; }
    }
    
    public Neuron(byte[] data,ref int outputOffset)
    {
        int length = BitConverter.ToInt32(data, outputOffset);
        outputOffset += sizeof(int);
        weights = new float[length];
        for (int i = 0; i < length; i++)
        {
            weights[i] = BitConverter.ToSingle(data, outputOffset);
            outputOffset += sizeof(float);
        }
        bias = BitConverter.ToSingle(data, outputOffset);
        outputOffset += sizeof(float);
        p = BitConverter.ToSingle(data, outputOffset);
        outputOffset += sizeof(float);
        
    }
    public byte[] Serialize()
    {
        List<byte> bytes = new List<byte>();
        
        bytes.AddRange(BitConverter.GetBytes(weights.Length));
        
        foreach (float weight in weights)
        {
            bytes.AddRange(BitConverter.GetBytes(weight));
        }
        
        bytes.AddRange(BitConverter.GetBytes(bias));
        
        bytes.AddRange(BitConverter.GetBytes(p));

        return bytes.ToArray();
    }

    public Neuron(int weightsCount, float bias, float p)
    {
        weights = new float[weightsCount];
        Random random = new Random();
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = (float)(random.NextDouble() * 2 - 1);
        }

        this.bias = bias;
        this.p = p;
    }

    public float Synapsis(float[] input)
    {
        float a = 0;

        for (int i = 0; i < input.Length; i++)
        {
            a += weights[i] * input[i];
        }

        a += bias * weights[weights.Length - 1];

        return Sigmoid(a, p);
    }

    public int SetWeights(float[] newWeights, int fromId)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            this.weights[i] = newWeights[i + fromId];
        }

        return fromId + weights.Length;
    }

    public float[] GetWeights()
    {
        return this.weights;
    }

    public static float Sigmoid(float a, float p)
    {
        return (float)Math.Tanh(a / p);
    }
}