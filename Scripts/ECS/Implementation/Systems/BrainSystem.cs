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
            inputComponents[entity].inputs = outputsComponents[entity].outputs;

            for (int layer = 0; layer < hiddenLayerComponents[entity].hiddenLayers.Length; layer++)
            {
                outputsComponents[entity].outputs = LayerSynapsis(entity, inputComponents[entity].inputs, layer);
                inputComponents[entity].inputs = outputsComponents[entity].outputs;
            }
            outputsComponents[entity].outputs = inputComponents[entity].inputs;
            outputsComponents[entity].outputs = OutputLayerSynapsis(entity, inputComponents[entity].inputs);
        });
    }

    private float[] LayerSynapsis(uint entity, float[] inputs, int layer)
    {
        int neuronCount = hiddenLayerComponents[entity].hiddenLayers[layer].weights.GetLength(0);
        outputsComponents[entity].outputs = new float[neuronCount];
        Parallel.For(0, neuronCount,parallelOptions, 
            neuron => {outputsComponents[entity].outputs[neuron] = NeuronSynapsis(entity, neuron, inputs, layer); });
        return outputsComponents[entity].outputs;
    }

    private float[] FirstLayerSynapsis(uint entity, float[] inputs)
    {
        Parallel.For(0, inputs.Length,parallelOptions, 
            neuron => { outputsComponents[entity].outputs[neuron] = FirstNeuronSynapsis(entity, neuron, inputs); });
        return outputsComponents[entity].outputs;
    }

    private float[] OutputLayerSynapsis(uint entity, float[] inputs)
    {
        int neuronCount = outputsLayerComponents[entity].layer.weights.GetLength(0);
        outputsComponents[entity].outputs = new float[neuronCount];
        Parallel.For(0, neuronCount,parallelOptions, 
            neuron => { outputsComponents[entity].outputs[neuron] = LastNeuronSynapsis(entity, neuron, inputs); });
        return outputsComponents[entity].outputs;
    }

    private float NeuronSynapsis(uint entity, int neuron, float[] inputs, int layer)
    {

        var bag = new ConcurrentBag<float>();
        float a = 0;
        Parallel.For(0, hiddenLayerComponents[entity].hiddenLayers[layer].weights.GetLength(1),parallelOptions, 
            k =>
            {
                bag.Add(hiddenLayerComponents[entity].hiddenLayers[layer].weights[neuron, k] * inputs[k]);
            });
        a = bag.Sum();
        a += biasComponents[entity].X;

        return 1.0f / (1.0f + (float)Math.Exp(-a / sigmoidComponents[entity].X));
    }

    private float LastNeuronSynapsis(uint entity, int neuron, float[] inputs)
    {
    
        
        var bag = new ConcurrentBag<float>();
        float a = 0;
        int exclusive = outputsLayerComponents[entity].layer.weights.GetLength(1);
        Parallel.For(0,  exclusive,parallelOptions, 
            k =>
            {
                bag.Add( outputsLayerComponents[entity].layer.weights[neuron, k] * inputs[k]
                );
            });
        a = bag.Sum();
        a += biasComponents[entity].X;

        return 1.0f / (1.0f + (float)Math.Exp(-a / sigmoidComponents[entity].X));
    }

    private float FirstNeuronSynapsis(uint entity, int neuron, float[] inputs)
    {
        var bag = new ConcurrentBag<float>();
        float a = 0;
        Parallel.For(0, inputs.Length,parallelOptions, 
            k =>
            {
                bag.Add(inputLayerComponents[entity].layer.weights[neuron, k] * inputs[k]);
            });
        a = bag.Sum();
        a += biasComponents[entity].X;

        return 1.0f / (1.0f + (float)Math.Exp(-a / sigmoidComponents[entity].X));
    }

    protected override void PostExecute(float deltaTime)
    {
    }
}