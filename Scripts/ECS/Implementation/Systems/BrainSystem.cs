using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class BrainSystem : ECSSystem
{
    private ParallelOptions parallelOptions;

    private IDictionary<uint, BiasComponent> biasComponents;
    private IDictionary<uint, SigmoidComponent> sigmoidComponents;
    private IDictionary<uint, InputLayerComponent> inputLayerComponents;
    private IDictionary<uint, HiddenLayerComponent> hiddenLayerComponents;
    private IDictionary<uint, OutputLayerComponent> outputsLayerComponents;
    private IDictionary<uint, OutputComponent> outputsComponents;
    private IDictionary<uint, InputComponent> inputComponents;
    private IEnumerable<uint> queriedEntities;

    public override void Initialize()
    {
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 32 };
    }

    protected override void PreExecute(float deltaTime)
    {
        sigmoidComponents ??= ECSManager.GetComponents<SigmoidComponent>();
        inputLayerComponents ??= ECSManager.GetComponents<InputLayerComponent>();
        inputComponents ??= ECSManager.GetComponents<InputComponent>();
        biasComponents ??= ECSManager.GetComponents<BiasComponent>();
        outputsLayerComponents ??= ECSManager.GetComponents<OutputLayerComponent>();
        outputsComponents ??= ECSManager.GetComponents<OutputComponent>();
        hiddenLayerComponents ??= ECSManager.GetComponents<HiddenLayerComponent>();
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(
            typeof(SigmoidComponent),
            typeof(InputLayerComponent),
            typeof(InputComponent),
            typeof(BiasComponent),
            typeof(OutputLayerComponent),
            typeof(OutputComponent),
            typeof(HiddenLayerComponent));
    }

    protected override void Execute(float deltaTime)
    {
        Parallel.ForEach(queriedEntities, parallelOptions, entity =>
        {
            outputsComponents[entity].outputs = new float[inputComponents[entity].inputs.Length];

            outputsComponents[entity].outputs = FirstLayerSynapsis(entity, inputComponents[entity].inputs);
            inputComponents[entity].size = outputsComponents[entity].outputs.Length;
            inputComponents[entity].inputs = outputsComponents[entity].outputs;
            outputsComponents[entity].outputs = new float[hiddenLayerComponents[entity].HiggestLayerSize];
            for (int layer = 0; layer < hiddenLayerComponents[entity].hiddenLayers.Length; layer++)
            {
                LayerSynapsis(entity, inputComponents[entity].inputs, layer, ref inputComponents[entity].size);
                inputComponents[entity].inputs = outputsComponents[entity].outputs;
            }

            outputsComponents[entity].outputs = OutputLayerSynapsis(entity, inputComponents[entity].inputs,
                ref inputComponents[entity].size);
        });
    }

    private float[] LayerSynapsis(uint entity, float[] inputs, int layer, ref int size)
    {
        int neuronCount = hiddenLayerComponents[entity].hiddenLayers[layer].weights.GetLength(0);
        Array.Resize(ref outputsComponents[entity].outputs, neuronCount);

        Parallel.For(0, neuronCount, parallelOptions,
            neuron => { outputsComponents[entity].outputs[neuron] = NeuronSynapsis(entity, neuron, inputs, layer); });

        size = neuronCount;
        return outputsComponents[entity].outputs;
    }

    private float[] FirstLayerSynapsis(uint entity, float[] inputs)
    {
        Parallel.For(0, inputs.Length, parallelOptions,
            neuron => { outputsComponents[entity].outputs[neuron] = FirstNeuronSynapsis(entity, neuron, inputs); });
        return outputsComponents[entity].outputs;
    }

    private float[] OutputLayerSynapsis(uint entity, float[] inputs, ref int size)
    {
        int neuronCount = outputsLayerComponents[entity].layer.weights.GetLength(0);
        Array.Resize(ref outputsComponents[entity].outputs, neuronCount);
        Parallel.For(0, neuronCount, parallelOptions,
            neuron => { outputsComponents[entity].outputs[neuron] = LastNeuronSynapsis(entity, neuron, inputs); });
        return outputsComponents[entity].outputs;
    }

    private float NeuronSynapsis(uint entity, int neuron, float[] inputs, int layer)
    {

        float a = 0;

        int exclusive = hiddenLayerComponents[entity].hiddenLayers[layer].weights.GetLength(1);
       
        for (int k = 0; k < exclusive; k++)
        {
           a+= (hiddenLayerComponents[entity].hiddenLayers[layer].weights[neuron, k] * inputs[k]);

        }
        
        a += biasComponents[entity].X;

        return (float)Math.Tanh(a / sigmoidComponents[entity].X);
    }

    private float LastNeuronSynapsis(uint entity, int neuron, float[] inputs)
    {
        float a = 0;
        int exclusive = outputsLayerComponents[entity].layer.weights.GetLength(1);

        for (int k = 0; k < exclusive; k++)
        {
            a += outputsLayerComponents[entity].layer.weights[neuron, k] * inputs[k];
        }

        a += biasComponents[entity].X;

        return (float)Math.Tanh(a / sigmoidComponents[entity].X);
    }

    private float FirstNeuronSynapsis(uint entity, int neuron, float[] inputs)
    {
        float a = 0;
        for (int k = 0; k < inputs.Length; k++)
        {
            a += inputLayerComponents[entity].layer.weights[neuron, k] * inputs[k];
        }

        a += biasComponents[entity].X;


        return (float)Math.Tanh(a / sigmoidComponents[entity].X);
        return 1.0f / (1.0f + (float)Math.Exp(-a / sigmoidComponents[entity].X));
    }

    protected override void PostExecute(float deltaTime)
    {
    }
}