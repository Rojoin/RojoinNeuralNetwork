using System;
using System.Collections.Generic;
using RojoinSaveSystem.Attributes;

[Serializable]
public class NeuronLayer
{
    [SaveValue(0)] public Neuron[] neurons = Array.Empty<Neuron>();
    private float[] outputs = Array.Empty<float>();
    public int totalWeights = 0;
    public int inputsCount = 0;
    [SaveValue(2)] private float bias = 1;
    [SaveValue(3)] private float p = 0.5f;

    public int NeuronsCount
    {
        get { return neurons.Length; }
    }

    public int InputsCount
    {
        get { return inputsCount; }
    }

    public int OutputsCount
    {
        get { return outputs.Length; }
    }

    public byte[] Serialize()
    {
        List<byte> bytes = new List<byte>();

        bytes.AddRange(BitConverter.GetBytes(neurons.Length));

        foreach (var neuron in neurons)
        {
            bytes.AddRange(neuron.Serialize());
        }

        bytes.AddRange(BitConverter.GetBytes(bias));
        bytes.AddRange(BitConverter.GetBytes(p));


        return bytes.ToArray();
    }

    public NeuronLayer(int inputsCount, int neuronsCount, float bias, float p)
    {
        this.inputsCount = inputsCount;
        this.bias = bias;
        this.p = p;

        SetNeuronsCount(neuronsCount);
    }

    public NeuronLayer(byte[] data, ref int output)
    {
        int length = BitConverter.ToInt32(data, output);
        output += sizeof(int);
        neurons = new Neuron[length];
        for (int i = 0; i < length; i++)
        {
            neurons[i] = new Neuron(data, ref output);
        }

        bias = BitConverter.ToSingle(data, output);
        output += sizeof(float);
        p = BitConverter.ToSingle(data, output);
        output += sizeof(float);
 
    }

    void SetNeuronsCount(int neuronsCount)
    {
        neurons = new Neuron[neuronsCount];

        for (int i = 0; i < neurons.Length; i++)
        {
            neurons[i] = new Neuron(inputsCount, bias, p);
            totalWeights += inputsCount;
        }

        outputs = new float[neurons.Length];
    }

    public int SetWeights(float[] weights, int fromId)
    {
        for (int i = 0; i < neurons.Length; i++)
        {
            fromId = neurons[i].SetWeights(weights, fromId);
        }

        return fromId;
    }

    public float[] GetWeights()
    {
        float[] weights = new float[totalWeights];
        int id = 0;

        for (int i = 0; i < neurons.Length; i++)
        {
            float[] ws = neurons[i].GetWeights();

            for (int j = 0; j < ws.Length; j++)
            {
                weights[id] = ws[j];
                id++;
            }
        }

        return weights;
    }

    public int GetWeightCount()
    {
        int id = 0;

        foreach (var neuron in neurons)
        {
            id += neuron.GetWeights().Length;
        }

        return id;
    }

    public float[] Synapsis(float[] inputs)
    {
        for (int j = 0; j < neurons.Length; j++)
        {
            outputs[j] = neurons[j].Synapsis(inputs);
        }

        return outputs;
    }
}