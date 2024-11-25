using System;
using System.Collections.Generic;
using RojoinNeuralNetwork.Save;
using RojoinNeuralNetwork.Scripts.Agents;
using RojoinSaveSystem;
using RojoinSaveSystem.Attributes;
using Random = System.Random;
using SaveSystem = RojoinNeuralNetwork.Save.SaveSystem;
using Vector2 = System.Numerics.Vector2;

namespace RojoinNeuralNetwork
{
    [System.Serializable]
    public class SporeManagerLib : ISaveObject, IManager
    {
        private SaveObjectData saveObject = new SaveObjectData();
        [SaveValue(0)] public int generation = 0;
        [SaveValue(1)] public int hervivoreCount = 30;
        [SaveValue(2)] public int carnivoreCount = 20;
        [SaveValue(3)] public int scavengerCount = 20;
        public int gridSizeX = 10;
        public int gridSizeY = 10;

        public int EliteCount = 4;
        public float MutationChance = 0.10f;
        public float MutationRate = 0.01f;
        public int turnCount = 100;
        private int currentTurn = 0;

        private Dictionary<Vector2, Plant> plantPositions = new Dictionary<Vector2, Plant>();
        public List<Herbivore> herbis = new List<Herbivore>();
        public List<Plant> plants = new List<Plant>();
        public List<Carnivore> carnivores = new List<Carnivore>();
        public List<Scavenger> scavengers = new List<Scavenger>();

        public List<Brain> herbMainBrains = new List<Brain>();
        public List<Brain> herbEatBrains = new List<Brain>();
        public List<Brain> herbMoveBrains = new List<Brain>();
        public List<Brain> herbEscapeBrains = new List<Brain>();
        public List<Brain> carnMainBrains = new List<Brain>();
        public List<Brain> carnMoveBrains = new List<Brain>();
        public List<Brain> carnEatBrains = new List<Brain>();
        public List<Brain> scavMainBrains = new List<Brain>();
        public List<Brain> scavFlokingBrains = new List<Brain>();
        List<BrainData> herbBrainData;
        List<BrainData> carnivoreBrainData;
        List<BrainData> scavBrainData;

        [SaveValue(4)] public GeneticAlgorithmData HMainB = new GeneticAlgorithmData();
        [SaveValue(5)] public GeneticAlgorithmData HEatB = new GeneticAlgorithmData();
        [SaveValue(6)] public GeneticAlgorithmData HEscapeB = new GeneticAlgorithmData();
        [SaveValue(7)] public GeneticAlgorithmData HMoveB = new GeneticAlgorithmData();
        [SaveValue(8)] public GeneticAlgorithmData CMainB = new GeneticAlgorithmData();
        [SaveValue(9)] public GeneticAlgorithmData CEatB = new GeneticAlgorithmData();
        [SaveValue(10)] public GeneticAlgorithmData CMoveB = new GeneticAlgorithmData();
        [SaveValue(11)] public GeneticAlgorithmData SMainB = new GeneticAlgorithmData();
        [SaveValue(12)] public GeneticAlgorithmData SFlockB = new GeneticAlgorithmData();

        public string fileToLoad;
        public string filepath;
        public string fileType;

        public List<GeneticAlgorithmData> data = new List<GeneticAlgorithmData>();
        Random random = new Random();

        public bool isActive;
        private Dictionary<uint, Brain> entities;
        private SaveSystem.GeneticAlgorithmDataManager manager = new SaveSystem.GeneticAlgorithmDataManager();

        public SporeManagerLib()
        {
        }

        public SporeManagerLib(List<BrainData> herbBrainData, List<BrainData> carnivoreBrainData,
            List<BrainData> scavBrainData, int gridSizeX, int gridSizeY, int hervivoreCount, int carnivoreCount,
            int scavengerCount, int turnCount)
        {
            this.herbBrainData = herbBrainData;
            this.carnivoreBrainData = carnivoreBrainData;
            this.scavBrainData = scavBrainData;
            this.turnCount = turnCount;
            this.gridSizeX = gridSizeX;
            this.gridSizeY = gridSizeY;
            this.hervivoreCount = hervivoreCount;
            this.carnivoreCount = carnivoreCount;
            this.scavengerCount = scavengerCount;


            const int SCAV_BRAINS = 2;
            const int CARN_BRAINS = 3;
            const int HERB_BRAINS = 4;
            if (herbBrainData.Count != HERB_BRAINS || carnivoreBrainData.Count != CARN_BRAINS ||
                scavBrainData.Count != SCAV_BRAINS)
            {
                throw new Exception(
                    "The brainData is invalid. The herbivore data should be: 4, carnivore data should be: 3, scav data should be: 2");
            }

            CreateAgents();
            ECSManager.Init();
            entities = new Dictionary<uint, Brain>();
            InitEntities();
            CreateNewGeneration();
        }

        public virtual void Tick(float deltaTime)
        {
            if (!isActive)
                return;
            if (currentTurn < turnCount)
            {
                PreUpdateAgents(deltaTime);
                UpdateInputs();
                ECSManager.Tick(deltaTime);
                AfterTick(deltaTime);
                currentTurn++;
            }
            else
            {
                EpochAllBrains();
                CreateNewGeneration();
            }
        }

        private void CreateNewGeneration()
        {
            generation++;
            ResetPositions();

            currentTurn = 0;
        }

        private void ResetPositions()
        {
            foreach (var herb in herbis)
            {
                herb.Reset(GetRandomHerbPosition());
            }

            foreach (var carn in carnivores)
            {
                carn.Reset(GetRandomCarnivorePosition());
            }

            foreach (var scav in scavengers)
            {
                scav.Reset(GetRandomScavPosition());
            }

            plantPositions.Clear();
            foreach (var plant in plants)
            {
                plant.Reset(GetRandomPlantPosition(plant));
            }
        }

        private Vector2 GetRandomHerbPosition()
        {
            float randomX = random.Next(0, gridSizeX);
            float randomY = random.Next(0, gridSizeY / 2);
            return new Vector2(randomX, randomY);
        }

        private Vector2 GetRandomCarnivorePosition()
        {
            float randomX = random.Next(0, gridSizeX);
            float randomY = random.Next(gridSizeY / 2, gridSizeY);
            return new Vector2(randomX, randomY);
        }

        private Vector2 GetRandomScavPosition()
        {
            float randomX = random.Next(0, gridSizeX);
            float randomY = random.Next(0, gridSizeY);
            return new Vector2(randomX, randomY);
        }

        private Vector2 GetRandomPlantPosition(Plant plant)
        {
            float randomX = random.Next(0, gridSizeX);
            float randomY = random.Next(0, gridSizeY);

            Vector2 randomPlantPosition = new Vector2(randomX, randomY);
            while (plantPositions.ContainsKey(randomPlantPosition))
            {
                randomX = random.Next(0, gridSizeX);
                randomY = random.Next(0, gridSizeY);

                randomPlantPosition.X = randomX;
                randomPlantPosition.Y = randomY;
            }

            plantPositions.Add(randomPlantPosition, plant);

            return randomPlantPosition;
        }

        private void InitEntities()
        {
            for (int i = 0; i < hervivoreCount; i++)
            {
                CreateEntity(herbis[i].mainBrain);
                CreateEntity(herbis[i].moveBrain);
                CreateEntity(herbis[i].eatBrain);
                CreateEntity(herbis[i].escapeBrain);
            }

            for (int i = 0; i < carnivoreCount; i++)
            {
                CreateEntity(carnivores[i].mainBrain);
                CreateEntity(carnivores[i].moveBrain);
                CreateEntity(carnivores[i].eatBrain);
            }

            for (int i = 0; i < scavengerCount; i++)
            {
                CreateEntity(scavengers[i].mainBrain);
                CreateEntity(scavengers[i].flockingBrain);
            }
        }

        private void CreateAgents()
        {
            for (int i = 0; i < hervivoreCount; i++)
            {
                herbis.Add(new Herbivore(this, herbBrainData[0].ToBrain(), herbBrainData[1].ToBrain(),
                    herbBrainData[2].ToBrain(), herbBrainData[3].ToBrain()));
                herbMainBrains.Add(herbis[i].mainBrain);
                herbMoveBrains.Add(herbis[i].moveBrain);
                herbEatBrains.Add(herbis[i].eatBrain);
                herbEscapeBrains.Add(herbis[i].escapeBrain);
            }


            for (int i = 0; i < carnivoreCount; i++)
            {
                carnivores.Add(new Carnivore(this, carnivoreBrainData[0].ToBrain(), carnivoreBrainData[1].ToBrain(),
                    carnivoreBrainData[2].ToBrain()));
                carnMainBrains.Add(carnivores[i].mainBrain);
                carnEatBrains.Add(carnivores[i].eatBrain);
                carnMoveBrains.Add(carnivores[i].moveBrain);
            }


            for (int i = 0; i < scavengerCount; i++)
            {
                scavengers.Add(new Scavenger(this, scavBrainData[0].ToBrain(), scavBrainData[1].ToBrain()));
                scavMainBrains.Add(scavengers[i].mainBrain);
                scavFlokingBrains.Add(scavengers[i].flockingBrain);
            }

            HMainB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, herbMainBrains[0]);
            HEatB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, herbEatBrains[0]);
            HEscapeB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, herbEscapeBrains[0]);
            HMoveB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, herbMoveBrains[0]);
            CMainB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, carnMainBrains[0]);
            CEatB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, carnEatBrains[0]);
            CMoveB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, carnMoveBrains[0]);
            SMainB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, scavMainBrains[0]);
            SFlockB = new GeneticAlgorithmData(EliteCount, MutationChance, MutationRate, scavFlokingBrains[0]);

            data.Add(HMainB);
            data.Add(HEatB);
            data.Add(HEscapeB);
            data.Add(HMoveB);
            data.Add(CMainB);
            data.Add(CEatB);
            data.Add(CMoveB);
            data.Add(SMainB);
            data.Add(SFlockB);

            foreach (GeneticAlgorithmData algorithmData in data)
            {
                manager.AddDataset(algorithmData);
            }

            for (int i = 0; i < hervivoreCount * 2; i++)
            {
                plants.Add(new Plant());
            }
        }

        private void CreateEntity(Brain brain)
        {
            uint entityID = ECSManager.CreateEntity();
            ECSManager.AddComponent<BiasComponent>(entityID, new BiasComponent(brain.bias));
            ECSManager.AddComponent<SigmoidComponent>(entityID, new SigmoidComponent(brain.p));
            ECSManager.AddComponent<InputLayerComponent>(entityID, new InputLayerComponent(brain.GetInputLayer()));
            ECSManager.AddComponent<HiddenLayerComponent>(entityID, new HiddenLayerComponent(brain.GetHiddenLayers()));
            ECSManager.AddComponent<OutputLayerComponent>(entityID, new OutputLayerComponent(brain.GetOutputLayer()));
            ECSManager.AddComponent<OutputComponent>(entityID, new OutputComponent(brain.outputs));
            ECSManager.AddComponent<InputComponent>(entityID, new InputComponent(brain.inputs, brain.InputsCount));
            entities.Add(entityID, brain);
        }

        #region Epoch

        private void EpochAllBrains()
        {
            foreach (var geneticAlgorithmData in data)
            {
                geneticAlgorithmData.generationCount = generation;
            }

            EpochHerbivore();
            EpochCarnivore();
            EpochScavenger();

            string file = $"{filepath}{generation}.{fileType}";
            manager.SaveAll(file);
            foreach (KeyValuePair<uint, Brain> entity in entities)
            {
                HiddenLayerComponent inputComponent = ECSManager.GetComponent<HiddenLayerComponent>(entity.Key);
                inputComponent.hiddenLayers = entity.Value.GetHiddenLayers();
                OutputLayerComponent outputComponent = ECSManager.GetComponent<OutputLayerComponent>(entity.Key);
                outputComponent.layer = entity.Value.GetOutputLayer();
            }
        }

        private void EpochScavenger()
        {
            int count = 0;
            foreach (var scav in scavengers)
            {
                scav.GiveFitnessToMain();
                if (scav.hasEaten)
                {
                    count++;
                }
                else
                {
                    scav.mainBrain.DestroyFitness();
                    scav.flockingBrain.DestroyFitness();
                }
            }

            bool isGenerationDead = count <= 1;

            EpochLocal(scavMainBrains, isGenerationDead, SMainB);
            EpochLocal(scavFlokingBrains, isGenerationDead, SFlockB);
        }

        void EpochCarnivore()
        {
            int count = 0;
            foreach (var carnivore in carnivores)
            {
                carnivore.GiveFitnessToMain();
                if (carnivore.hasEatenEnoughFood)
                {
                    count++;
                }
                else
                {
                    carnivore.mainBrain.DestroyFitness();
                    carnivore.eatBrain.DestroyFitness();
                    carnivore.moveBrain.DestroyFitness();
                }
            }

            bool isGenerationDead = count <= 1;


            EpochLocal(carnMainBrains, isGenerationDead, CMainB);
            EpochLocal(carnEatBrains, isGenerationDead, CEatB);
            EpochLocal(carnMoveBrains, isGenerationDead, CMoveB);
        }

        private void EpochHerbivore()
        {
            int count = 0;
            foreach (Herbivore herbivore in herbis)
            {
                herbivore.GiveFitnessToMain();
                if (herbivore.lives > 0 && herbivore.hasEatenFood)
                {
                    count++;
                }
                else
                {
                    herbivore.mainBrain.DestroyFitness();
                    herbivore.eatBrain.DestroyFitness();
                    herbivore.escapeBrain.DestroyFitness();
                    herbivore.moveBrain.DestroyFitness();
                }
            }

            bool isGenerationDead = count <= 1;

            EpochLocal(herbMainBrains, isGenerationDead, HMainB);
            EpochLocal(herbMoveBrains, isGenerationDead, HMoveB);
            EpochLocal(herbEatBrains, isGenerationDead, HEatB);
            EpochLocal(herbEscapeBrains, isGenerationDead, HEscapeB);
        }

        private void EpochLocal(List<Brain> brains, bool force, GeneticAlgorithmData info)
        {
            Genome[] newGenomes = GeneticAlgorithm.Epoch(GetGenomes(brains), info, force);
            info.lastGenome = newGenomes;
            for (int i = 0; i < brains.Count; i++)
            {
                brains[i] = new Brain(info.brainStructure);
                brains[i].SetWeights(newGenomes[i].genome);
            }
        }

        private void RestoreSave()
        {
            List<GeneticAlgorithmData> dataToPaste = manager.GetAllDatasets();

            HMainB = dataToPaste[0];
            HEatB = dataToPaste[1];
            HEscapeB = dataToPaste[2];
            HMoveB = dataToPaste[3];
            CMainB = dataToPaste[4];
            CEatB = dataToPaste[5];
            CMoveB = dataToPaste[6];
            SMainB = dataToPaste[7];
            SFlockB = dataToPaste[8];
            manager.ClearDatasets();
            manager.AddDataset(HMainB);
            manager.AddDataset(HEatB);
            manager.AddDataset(HEscapeB);
            manager.AddDataset(HMoveB);
            manager.AddDataset(CMainB);
            manager.AddDataset(CEatB);
            manager.AddDataset(CMoveB);
            manager.AddDataset(SMainB);
            manager.AddDataset(SFlockB);


            generation = HMainB.generationCount;
            RestoreBrainsData(herbMainBrains, HMainB);
            RestoreBrainsData(herbMoveBrains, HMoveB);
            RestoreBrainsData(herbEatBrains, HEatB);
            RestoreBrainsData(herbEscapeBrains, HEscapeB);
            RestoreBrainsData(carnMainBrains, CMainB);
            RestoreBrainsData(carnEatBrains, CEatB);
            RestoreBrainsData(carnMoveBrains, CMoveB);
            RestoreBrainsData(scavMainBrains, SMainB);
            RestoreBrainsData(scavFlokingBrains, SFlockB);
            ResetPositions();
        }

        private void RestoreBrainsData(List<Brain> brains, GeneticAlgorithmData info)
        {
            for (int i = 0; i < brains.Count; i++)
            {
                // Brain brain =;
                brains[i] = new Brain(info.brainStructure);
                brains[i].SetWeights(info.lastGenome[i].genome);
            }
        }

        private static Genome[] GetGenomes(List<Brain> brains)
        {
            List<Genome> genomes = new List<Genome>();
            foreach (var brain in brains)
            {
                Genome genome = new Genome(brain.GetTotalWeightsCount());
                genomes.Add(genome);
            }

            return genomes.ToArray();
        }

        #endregion

        #region Updates

        private void PreUpdateAgents(float deltaTime)
        {
            foreach (Herbivore herbi in herbis)
            {
                herbi.PreUpdate(deltaTime);
            }

            foreach (Carnivore carn in carnivores)
            {
                carn.PreUpdate(deltaTime);
            }

            foreach (Scavenger scav in scavengers)
            {
                scav.PreUpdate(deltaTime);
            }
        }

        private void UpdateInputs()
        {
            foreach (KeyValuePair<uint, Brain> entity in entities)
            {
                InputComponent inputComponent = ECSManager.GetComponent<InputComponent>(entity.Key);
                inputComponent.inputs = entity.Value.inputs;
            }
        }

        public void AfterTick(float deltaTime = 0)
        {
            foreach (KeyValuePair<uint, Brain> entity in entities)
            {
                OutputComponent output = ECSManager.GetComponent<OutputComponent>(entity.Key);
                entity.Value.outputs = output.outputs;
            }

            foreach (Herbivore herbi in herbis)
            {
                herbi.Update(deltaTime);
            }

            foreach (Carnivore carn in carnivores)
            {
                carn.Update(deltaTime);
            }

            foreach (Scavenger scav in scavengers)
            {
                scav.Update(deltaTime);
            }
        }

        #endregion


        public int GetID()
        {
            return saveObject.id;
        }

        public ISaveObject GetObject()
        {
            return this;
        }

        public void Save()
        {
        }

        public void Load()
        {
            manager.LoadAll(fileToLoad);
            RestoreSave();
            isActive = false;
            foreach (var entity in entities)
            {
                ECSManager.GetComponent<BiasComponent>(entity.Key).X = entity.Value.bias;
                ECSManager.GetComponent<SigmoidComponent>(entity.Key).X = entity.Value.p;
                ECSManager.GetComponent<InputLayerComponent>(entity.Key).layer = entity.Value.GetInputLayer();
                HiddenLayerComponent hiddenLayerComponent = ECSManager.GetComponent<HiddenLayerComponent>(entity.Key);
                hiddenLayerComponent.hiddenLayers = entity.Value.GetHiddenLayers();
                hiddenLayerComponent.SetHighestLayerSize();
                ECSManager.GetComponent<OutputLayerComponent>(entity.Key).layer = entity.Value.GetOutputLayer();
                ECSManager.GetComponent<OutputComponent>(entity.Key).outputs = entity.Value.outputs;
                ECSManager.GetComponent<InputComponent>(entity.Key).inputs = entity.Value.inputs;
            }

            currentTurn = 0;
        }

        public virtual Herbivore GetNearHerbivore(Vector2 position)
        {
            Herbivore nearest = null;
            float distance = float.MaxValue;

            foreach (Herbivore go in herbis)
            {
                if (go.position == position)
                {
                    return go;
                }
                float newDist = Vector2.Distance(position, go.position);
                if (newDist < distance)
                {
                    nearest = go;
                    distance = newDist;
                }
            }

            return nearest;
        }

        public int GetGridX()
        {
            return gridSizeX;
        }

        public int GetGridY()
        {
            return gridSizeY;
        }

        public virtual Plant GetNearPlant(Vector2 position)
        {
            Plant nearest = null;

            float shortestDistance = float.MaxValue;
            if (plantPositions.TryGetValue(position, out Plant value))
            {
                if (value.isAvailable)
                {
                    return value;
                }
            }

            foreach (var plant in plantPositions)
            {
                if (plant.Value.isAvailable)
                {
                    float newDist = Vector2.Distance(position, plant.Value.position);

                    if (newDist < shortestDistance)
                    {
                        nearest = plant.Value;
                        shortestDistance = newDist;
                    }
                }
            }

            return nearest;
        }

        public List<Scavenger> GetNearScavs(Scavenger scavenger)
        {
            var nearbyScav = new List<(Scavenger scav, float distance)>();

            foreach (Scavenger go in scavengers)
            {
                if (go == scavenger)
                {
                    continue;
                }
                float newDist = Vector2.Distance(scavenger.position, go.position);

                nearbyScav.Add((go, newDist));
            }

            nearbyScav.Sort((a, b) => a.distance.CompareTo(b.distance));

            List<Scavenger> nearScav = new List<Scavenger>();
            nearScav.Add(nearbyScav[0].scav);
            nearScav.Add(nearbyScav[1].scav);
            nearScav.Add(nearbyScav[2].scav);
            return nearScav;
        }

        public List<Vector2> GetNearCarnivores(Vector2 position)
        {
            var nearCarn = new List<(Carnivore scav, float distance)>();

            foreach (Carnivore go in carnivores)
            {
                float newDist = Vector2.Distance(position, go.position);
                nearCarn.Add((go, newDist));
            }

            nearCarn.Sort((a, b) => a.distance.CompareTo(b.distance));

            List<Vector2> carnToReturn = new List<Vector2>();
            carnToReturn.Add(nearCarn[0].scav.position);
            carnToReturn.Add(nearCarn[1].scav.position);
            carnToReturn.Add(nearCarn[2].scav.position);
            return carnToReturn;
        }
    }

    public interface IManager
    {
        public List<Vector2> GetNearCarnivores(Vector2 position);
        public List<Scavenger> GetNearScavs(Scavenger scavenger);
        public Plant GetNearPlant(Vector2 position);
        public Herbivore GetNearHerbivore(Vector2 position);
        public int GetGridX();
        public int GetGridY();
    }
}