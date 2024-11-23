using System;
using System.Collections.Generic;

[Serializable]
public class Brain 
{

    public List<NeuronLayer> layers = new List<NeuronLayer>();
    int totalWeightsCount = 0;
    int inputsCount = 0;
    private float fitness = 1;
    public float FitnessReward;
    public float FitnessMultiplier;
    public float[] outputs;
    int fitnessCount = 0;

    public float bias = 1;
    public float p = 0.5f;
    public float[] inputs;

    public int InputsCount
    {
        get { return inputsCount; }
    }

    public Brain()
    {
    }

    public Brain(Brain brain)
    {
        bias = brain.bias;
        layers = layers;
        totalWeightsCount = brain.totalWeightsCount;
    }

    public void CopyStructureFrom(Brain brain)
    {
        layers = brain.layers;
    }

    public void ApplyFitness()
    {
        fitness *= FitnessReward * FitnessMultiplier > 0 ?FitnessReward+  FitnessMultiplier : 0;
    }
    public void DestroyFitness()
    {
        fitness *= 0;
    }

    public bool AddNeuronLayer(int neuronsCount, float bias, float p)
    {
        if (layers.Count == 0)
        {
            throw new Exception("Call AddFirstNeuronLayer(int inputsCount, float bias, float p) for the first layer.");
            return false;
        }

        return AddNeuronLayer(layers[layers.Count - 1].OutputsCount, neuronsCount, bias, p);
    }

    public bool AddFirstNeuronLayer
        (int inputsCount, float bias, float p)
    {
        if (layers.Count != 0)
        {
            throw new Exception("Call AddNeuronLayer(int neuronCount, float bias, float p) for the rest of the layers.");
            return false;
        }

        this.inputsCount = inputsCount;

        return AddNeuronLayer(inputsCount, inputsCount, bias, p);
    }

    private bool AddNeuronLayer(int inputsCount, int neuronsCount, float bias, float p)
    {
        if (layers.Count > 0 && layers[layers.Count - 1].OutputsCount != inputsCount)
        {
            throw new Exception("Inputs Count must match outputs from previous layer.");
            return false;
        }

        NeuronLayer layer = new NeuronLayer(inputsCount, neuronsCount, bias, p);

        totalWeightsCount += inputsCount * neuronsCount;

        layers.Add(layer);

        return true;
    }

    public bool AddNeuronLayerAtPosition(int neuronsCount, int layerPosition)
    {
        if (layers.Count <= 0 || layerPosition >= layers.Count)
        {
            throw new Exception("No previous Layer or out of range");
            return false;
        }

        NeuronLayer layer = new NeuronLayer(layers[layerPosition].OutputsCount, neuronsCount, bias, p);

        totalWeightsCount -= layers[layerPosition].OutputsCount * layers[layerPosition + 1].OutputsCount;

        layers[layerPosition + 1] = new NeuronLayer(neuronsCount, layers[layerPosition + 1].NeuronsCount, bias, p);


        totalWeightsCount += layers[layerPosition + 1].OutputsCount * neuronsCount;


        totalWeightsCount += layers[layerPosition].OutputsCount * neuronsCount;


        layers.Insert(layerPosition + 1, layer);

        totalWeightsCount = GetWeightsCount();


        return true;
    }

    public bool AddNeuronAtLayer(int neuronsCountToAdd, int layerPosition)
    {
        NeuronLayer oldLayer = layers[layerPosition];
        layers[layerPosition] =
            new NeuronLayer(oldLayer.InputsCount, oldLayer.NeuronsCount + neuronsCountToAdd, bias, p);


        NeuronLayer oldNextLayer = layers[layerPosition + 1];
        layers[layerPosition + 1] =
            new NeuronLayer(layers[layerPosition].OutputsCount, oldNextLayer.NeuronsCount, bias, p);


        totalWeightsCount += layers[layerPosition].OutputsCount * neuronsCountToAdd;
        totalWeightsCount += layers[layerPosition + 1].OutputsCount * neuronsCountToAdd;




        totalWeightsCount = GetWeightsCount();


        return true;
    }

    public int GetTotalWeightsCount()
    {
        return totalWeightsCount;
    }

    public void SetWeights(float[] newWeights)
    {
        int fromId = 0;

        for (int i = 0; i < layers.Count; i++)
        {
            fromId = layers[i].SetWeights(newWeights, fromId);
        }
    }

    public float[] GetWeights()
    {
        float[] weights = new float[totalWeightsCount];
        int id = 0;

        for (int i = 0; i < layers.Count; i++)
        {
            float[] ws = layers[i].GetWeights();

            for (int j = 0; j < ws.Length; j++)
            {
                weights[id] = ws[j];
                id++;
            }
        }

        return weights;
    }

    public int GetWeightsCount()
    {
        int id = 0;
        foreach (var layer in layers)
        {
            id += layer.GetWeightCount();
        }

        return id;
    }

    public float[] Synapsis(float[] inputs)
    {
        float[] outputs = null;

        for (int i = 0; i < layers.Count; i++)
        {
            outputs = layers[i].Synapsis(inputs);
            inputs = outputs;
        }

        return outputs;
    }

    public Layer GetInputLayer()
    {
        int id = layers[0].neurons.Length;
        float[,] weights = new float[layers[0].neurons.Length, layers[0].neurons[0].WeightsCount];
        for (int index = 0; index < layers[0].neurons.Length; index++)
        {
            for (int j = 0; j < layers[0].neurons[index].WeightsCount; j++)
            {
                weights[index, j] = layers[0].neurons[index].GetWeights()[j];
            }
        }

        Layer layer = new Layer(id, weights);
        return layer;
    }

    public Layer GetOutputLayer()
    {
        Index layerIndex = ^1;
        int id = layers[layerIndex].neurons.Length;
        float[,] weights = new float[layers[layerIndex].neurons.Length, layers[layerIndex].neurons[0].WeightsCount];
        for (int index = 0; index < layers[layerIndex].neurons.Length; index++)
        {
            for (int j = 0; j < layers[layerIndex].neurons[index].WeightsCount; j++)
            {
                weights[index, j] = layers[layerIndex].neurons[index].GetWeights()[j];
            }
        }

        Layer layer = new Layer(id, weights);
        return layer;
    }

    public Layer[] GetHiddenLayers()
    {
        Layer[] layersToReturn = new Layer[layers.Count - 2 > 0 ? layers.Count - 2 : 0];
        int count = 0;

        for (int k = 0; k < this.layers.Count; k++)
        {
            if (k == 0 || k == this.layers.Count - 1)
            {
                continue;
            }

            int id = layers[k].neurons.Length;
            float[,] weights = new float[layers[k].neurons.Length, layers[k].neurons[0].WeightsCount];
            for (int index = 0; index < layers[k].neurons.Length; index++)
            {
                for (int j = 0; j < layers[k].neurons[index].WeightsCount; j++)
                {
                    weights[index, j] = layers[k].neurons[index].GetWeights()[j];
                }
            }

            layersToReturn[count] = new Layer(id, weights);
            count++;
        }

        return layersToReturn;
    }

    public static Brain CreateBrain(int InputsCount, int HiddenLayers, int NeuronsCountPerHL, int OutputsCount,
        float Bias, float P)
    {
        Brain brain = new Brain();

        brain.AddFirstNeuronLayer(InputsCount, Bias, P);

        for (int i = 0; i < HiddenLayers; i++)
        {
            brain.AddNeuronLayer(NeuronsCountPerHL, Bias, P);
        }

        brain.AddNeuronLayer(OutputsCount, Bias, P);

        return brain;
    } 
    public static Brain CreateBrain(int InputsCount, int[] HiddenLayers, int OutputsCount,
        float Bias, float P)
    {
        Brain brain = new Brain();

        brain.AddFirstNeuronLayer(InputsCount, Bias, P);

        for (int i = 0; i < HiddenLayers.Length; i++)
        {
            brain.AddNeuronLayer(HiddenLayers[i], Bias, P);
        }

        brain.AddNeuronLayer(OutputsCount, Bias, P);

        return brain;
    }


}