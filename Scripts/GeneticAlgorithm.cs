using System;
using System.Collections.Generic;
using RojoinNeuralNetwork.Utils;
using RojoinSaveSystem.Attributes;

[Serializable]
public class GeneticAlgorithmData
{
    public float totalFitness = 0;
    public int eliteCount = 0;
    public float mutationChance = 0.0f;
    public float mutationRate = 0.0f;
    public readonly int maxStalledGenerationsUntilEvolve = 5;
    [SaveValue(0)] public Brain brainStructure;
    [SaveValue(1)] public Genome[] lastGenome = Array.Empty<Genome>();
    [SaveValue(2)] public int generationStalled = 0;
    [SaveValue(3)] public int generationCount = 0;

    public GeneticAlgorithmData()
    {
        eliteCount = 5;
        mutationChance = 0.2f;
        mutationRate = 0.4f;
    }

    public GeneticAlgorithmData(byte[] data, ref int offset)
    {
        eliteCount = BitConverter.ToInt32(data, offset);
        offset += sizeof(int);

        mutationChance = BitConverter.ToSingle(data, offset);
        offset += sizeof(float);

        mutationRate = BitConverter.ToSingle(data, offset);
        offset += sizeof(float);

        brainStructure = new Brain(data, ref offset);

        lastGenome = CreateGenomeArray(data, ref offset);

        generationStalled = BitConverter.ToInt32(data, offset);
        offset += sizeof(int);

        generationCount = BitConverter.ToInt32(data, offset);
        offset += sizeof(int);
    }

    public byte[] Serialize()
    {
        List<byte> bytes = new List<byte>();

        bytes.AddRange(BitConverter.GetBytes(eliteCount));

        bytes.AddRange(BitConverter.GetBytes(mutationChance));

        bytes.AddRange(BitConverter.GetBytes(mutationRate));

        bytes.AddRange(brainStructure.Serialize());

        bytes.AddRange(SerializeGenomeArray(lastGenome));

        bytes.AddRange(BitConverter.GetBytes(generationStalled));


        bytes.AddRange(BitConverter.GetBytes(generationCount));

        return bytes.ToArray();
    }

    private Genome[] CreateGenomeArray(byte[] data, ref int currentOffset)
    {
        int arrayLength = BitConverter.ToInt32(data, currentOffset);
        currentOffset += sizeof(int);


        Genome[] genomes = new Genome[arrayLength];


        for (int i = 0; i < arrayLength; i++)
        {
            genomes[i] = new Genome(data, ref currentOffset);
        }

        return genomes;
    }

    private byte[] SerializeGenomeArray(Genome[] genomes)
    {
        List<byte> bytes = new List<byte>();

        bytes.AddRange(BitConverter.GetBytes(genomes.Length));

        foreach (var genome in genomes)
        {
            bytes.AddRange(genome.Serialize());
        }

        return bytes.ToArray();
    }

    public GeneticAlgorithmData(int eliteCount, float mutationChance, float mutationRate, Brain brain,
        int maxStalledGenerationsUntilEvolve = 5)
    {
        this.eliteCount = eliteCount;
        this.mutationChance = mutationChance;
        this.mutationRate = mutationRate;
        this.brainStructure = brain;
        this.maxStalledGenerationsUntilEvolve = maxStalledGenerationsUntilEvolve;
    }

    public GeneticAlgorithmData(GeneticAlgorithmData data)
    {
        this.eliteCount = data.eliteCount;
        this.mutationChance = data.mutationChance;
        this.mutationRate = data.mutationRate;
        this.brainStructure = data.brainStructure;
        this.maxStalledGenerationsUntilEvolve = data.maxStalledGenerationsUntilEvolve;
    }


    public void Save()
    {
    }

    public void Load()
    {
    }
}

[Serializable]
public static class GeneticAlgorithm
{
    private static Random random = new Random();

    enum EvolutionType
    {
        None = 0,
        AddNeurons,
        AddLayer
    }

    public static List<Genome> population = new List<Genome>();
    static List<Genome> newPopulation = new List<Genome>();

    private static int newNeuronToAddQuantity;
    private static int randomLayer = 0;
    private static List<NeuronLayer> neuronLayers;


    public static Genome[] GetRandomGenomes(int count, int genesCount)
    {
        Genome[] genomes = new Genome[count];

        for (int i = 0; i < count; i++)
        {
            genomes[i] = new Genome(genesCount);
        }

        return genomes;
    }

    public static float RandomRangeFloat(float min, float max)
    {
        return (float)(random.NextDouble() * (max - min) + min);
    }

    public static Genome[] Epoch(Genome[] oldGenomes, GeneticAlgorithmData data, bool forceEvolve = false)
    {
        float currentTotalFitness = 0;
        EvolutionType evolutionType = EvolutionType.None;

        population.Clear();
        newPopulation.Clear();

        population.AddRange(oldGenomes);
        population.Sort(HandleComparison);

        GeneticAlgorithmData backUpData = new GeneticAlgorithmData(data);
        foreach (Genome g in population)
        {
            currentTotalFitness += g.fitness;
        }


        if (forceEvolve)
        {
            data.generationStalled = 0;
            data.mutationChance *= 2.8f;
            data.mutationRate *= 2.8f;
            evolutionType = EvolutionType.AddLayer;
            //evolutionType = (EvolutionType)random.Next(1, Enum.GetValues(typeof(EvolutionType)).Length);
        }
        else if (currentTotalFitness < data.totalFitness || currentTotalFitness == 0)
        {
            data.generationStalled++;
            if (data.generationStalled >= data.maxStalledGenerationsUntilEvolve)
            {
                data.generationStalled = 0;
                evolutionType = (EvolutionType)random.Next(1, Enum.GetValues(typeof(EvolutionType)).Length);
            }
        }

        data.totalFitness = currentTotalFitness;
        CalculateNeuronsToAdd(data.brainStructure);


        SelectElite(evolutionType, data.eliteCount);
        while (newPopulation.Count < population.Count)
        {
            Crossover(data, evolutionType);
        }

        foreach (Genome genome in newPopulation)
        {
            switch (evolutionType)
            {
                case EvolutionType.None:
                    break;
                case EvolutionType.AddNeurons:
                    EvolveChildNeurons(genome);
                    break;
                case EvolutionType.AddLayer:
                    EvolveChildLayer(genome);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(evolutionType), evolutionType, null);
            }
        }

        switch (evolutionType)
        {
            case EvolutionType.None:
                break;
            case EvolutionType.AddNeurons:
                data.brainStructure.AddNeuronAtLayer(newNeuronToAddQuantity, randomLayer);
                break;
            case EvolutionType.AddLayer:
                data.brainStructure.AddNeuronLayerAtPosition(newNeuronToAddQuantity, randomLayer);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(evolutionType), evolutionType, null);
        }

        data.mutationChance = backUpData.mutationChance;
        data.mutationRate = backUpData.mutationRate;
        data.lastGenome = newPopulation.ToArray();
        return data.lastGenome;
    }

    private static void CalculateNeuronsToAdd(Brain brain)
    {
        newNeuronToAddQuantity = random.Next(2, 4);
        randomLayer = random.Next(1, brain.layers.Count - 1);
        neuronLayers = brain.layers;
    }


    static void SelectElite(EvolutionType evolutionType, int eliteCount)
    {
        for (int i = 0; i < eliteCount && newPopulation.Count < population.Count; i++)
        {
            newPopulation.Add(population[i]);
        }
    }

    static void Crossover(GeneticAlgorithmData data, EvolutionType evolutionType)
    {
        Genome mom = RouletteSelection(data.totalFitness);
        Genome dad = RouletteSelection(data.totalFitness);

        Genome child1;
        Genome child2;

        Crossover(data, evolutionType, mom, dad, out child1, out child2);

        newPopulation.Add(child1);
        newPopulation.Add(child2);
    }

    static void Crossover(GeneticAlgorithmData data, EvolutionType evolutionType, Genome mom, Genome dad,
        out Genome child1,
        out Genome child2)
    {
        child1 = new Genome();
        child2 = new Genome();

        child1.genome = new float[mom.genome.Length];
        child2.genome = new float[mom.genome.Length];
        Logger.Log($"CrossOver Genome Length: {mom.genome.Length}");
        int pivot = random.Next(0, mom.genome.Length);

        for (int i = 0; i < pivot; i++)
        {
            child1.genome[i] = mom.genome[i];

            if (ShouldMutate(data.mutationChance))
                child1.genome[i] += RandomRangeFloat(-data.mutationRate, data.mutationRate);

            child2.genome[i] = dad.genome[i];

            if (ShouldMutate(data.mutationChance))
                child2.genome[i] += RandomRangeFloat(-data.mutationRate, data.mutationRate);
        }


        for (int i = pivot; i < mom.genome.Length; i++)
        {
            child2.genome[i] = mom.genome[i];

            if (ShouldMutate(data.mutationChance))
                child2.genome[i] += RandomRangeFloat(-data.mutationRate, data.mutationRate);

            child1.genome[i] = dad.genome[i];

            if (ShouldMutate(data.mutationChance))
                child1.genome[i] += RandomRangeFloat(-data.mutationRate, data.mutationRate);
        }
        
    }

    static bool ShouldMutate(float mutationChance)
    {
        return RandomRangeFloat(0.0f, 1.0f) < mutationChance;
    }

    static int HandleComparison(Genome x, Genome y)
    {
        return x.fitness > y.fitness ? 1 : x.fitness < y.fitness ? -1 : 0;
    }

    static void EvolveChildNeurons(Genome child)
    {
        int previousLayerOutputs = neuronLayers[randomLayer].OutputsCount;
        int nextLayerOutputs = neuronLayers[randomLayer + 1].OutputsCount;

        int newNeuronCount = child.genome.Length
                             + newNeuronToAddQuantity * neuronLayers[randomLayer].InputsCount +
                             nextLayerOutputs * newNeuronToAddQuantity;
        float[] newWeight = new float[newNeuronCount];

        int count = 0;
        int originalWeightsCount = 0;


        for (int i = 0; i < randomLayer; i++)
        {
            for (int w = 0; w < neuronLayers[i].GetWeightCount(); w++)
            {
                CopyExistingWeights(ref count, ref originalWeightsCount);
            }
        }


        for (int i = 0; i < neuronLayers[randomLayer].InputsCount; i++)
        {
            for (int j = 0; j < previousLayerOutputs + newNeuronToAddQuantity; j++)
            {
                if (j < previousLayerOutputs)
                {
                    CopyExistingWeights(ref count, ref originalWeightsCount);
                }
                else
                {
                    CreateNewWeights(ref count);
                }
            }
        }

        for (int i = 0; i < previousLayerOutputs + newNeuronToAddQuantity; i++)
        {
            for (int j = 0; j < nextLayerOutputs; j++)
            {
                if (i < previousLayerOutputs)
                {
                    CopyExistingWeights(ref count, ref originalWeightsCount);
                }
                else
                {
                    CreateNewWeights(ref count);
                }
            }
        }

        while (count < newNeuronCount)
        {
            CopyExistingWeights(ref count, ref originalWeightsCount);
        }

        child.genome = newWeight;
        return;


        void CopyExistingWeights(ref int count, ref int originalWeightsCount)
        {
            newWeight[count] = child.genome[originalWeightsCount];
            originalWeightsCount++;
            count++;
        }

        void CreateNewWeights(ref int count)
        {
            newWeight[count] = RandomRangeFloat(-1.0f, 1.0f);
            count++;
        }
    }

    static void EvolveChildLayer(Genome child)
    {
        //Neurona
        int count = 0;
        int originalWeightsCount = 0;


        int previousLayerInputs = neuronLayers[randomLayer].OutputsCount;
        int nextLayerInputs = neuronLayers[randomLayer + 1].OutputsCount;

        int oldConections = ((previousLayerInputs) * nextLayerInputs);

        int newTotalWeight = child.genome.Length - oldConections +
                             (previousLayerInputs * newNeuronToAddQuantity) +
                             (newNeuronToAddQuantity) * nextLayerInputs;


        float[] newWeight = new float[newTotalWeight];


        int weightsBeforeInsertion = 0;
        Logger.Log($"The genome length is {child.genome.Length}. New total weights:{newTotalWeight}");
        for (int layerIndex = 0; layerIndex < randomLayer; layerIndex++)
        {
            weightsBeforeInsertion += neuronLayers[layerIndex].GetWeightCount();
        }

        Logger.Log($"New total weights:{newTotalWeight} : weights before insertion: {weightsBeforeInsertion}");


        while (count < weightsBeforeInsertion)
        {
            CopyExistingWeights(ref count, ref originalWeightsCount);
        }


        for (int i = 0; i < previousLayerInputs; i++)
        {
            for (int j = 0; j < newNeuronToAddQuantity; j++)
            {
                CreateNewWeights(ref count);
            }
        }


        for (int i = 0; i < newNeuronToAddQuantity; i++)
        {
            for (int j = 0; j < nextLayerInputs; j++)
            {
                CreateNewWeights(ref count);
            }
        }

        while (count < newTotalWeight)
        {
            CopyExistingWeights(ref count, ref originalWeightsCount);
        }


        child.genome = newWeight;
        return;


        void CopyExistingWeights(ref int count, ref int originalWeightsCount)
        {
            newWeight[count] = child.genome[originalWeightsCount];
            originalWeightsCount++;
            count++;
        }

        void CreateNewWeights(ref int count)
        {
            if (count >= newWeight.Length)
            {
                Logger.Log($"<color=red>Count{count} exceeds weights{newWeight.Length}</color>");
            }

            newWeight[count] = RandomRangeFloat(-1.0f, 1.0f);
            count++;
        }
    }

    public static Genome RouletteSelection(float totalFitness)
    {
        float rnd = RandomRangeFloat(0, MathF.Max(totalFitness, 0));

        float fitness = 0;

        for (int i = 0; i < population.Count; i++)
        {
            fitness += MathF.Max(population[i].fitness, 0);
            if (fitness >= rnd)
                return population[i];
        }

        return null;
    }
}