using System;
using System.Collections.Generic;
using Random = System.Random;
using Vector2 = System.Numerics.Vector2;

namespace RojoinNeuralNetwork
{
    [System.Serializable]
    public class SporeManager
    {
        public int generation = 0;

        public int hervivoreCount = 30;
        public int carnivoreCount = 20;
        public int scavengerCount = 20;
        public int gridSizeX = 10;
        public int gridSizeY = 10;

        public int EliteCount = 4;
        public float MutationChance = 0.10f;
        public float MutationRate = 0.01f;
        public int turnCount = 100;
        private int currentTurn = 0;

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

        public GeneticAlgorithmData HMainB;
        public GeneticAlgorithmData HEatB;
        public GeneticAlgorithmData HEscapeB;
        public GeneticAlgorithmData HMoveB;
        public GeneticAlgorithmData CMainB;
        public GeneticAlgorithmData CEatB;
        public GeneticAlgorithmData CMoveB;
        public GeneticAlgorithmData SMainB;
        public GeneticAlgorithmData SFlockB;

        public string fileToLoad;
        public string filepath;
        private string filetype = "spore";
        public List<GeneticAlgorithmData> data = new List<GeneticAlgorithmData>();

        public bool isActive;
        private Dictionary<uint, Brain> entities;

        public SporeManager()
        {
        }

        public SporeManager(List<BrainData> herbBrainData, List<BrainData> carnivoreBrainData,
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

            foreach (var plant in plants)
            {
                plant.Reset(GetRandomScavPosition());
            }

            currentTurn = 0;
        }

        private Vector2 GetRandomHerbPosition()
        {
            Random random = new Random();
            float randomX = random.Next(0, gridSizeX);
            float randomY = random.Next(0, gridSizeY / 2);
            return new Vector2(randomX, randomY);
        }

        private Vector2 GetRandomCarnivorePosition()
        {
            Random random = new Random();
            float randomX = random.Next(0, gridSizeX);
            float randomY = random.Next(gridSizeY / 2, gridSizeY);
            return new Vector2(randomX, randomY);
        }

        private Vector2 GetRandomScavPosition()
        {
            Random random = new Random();
            float randomX = random.Next(0, gridSizeX);
            float randomY = random.Next(0, gridSizeY);
            return new Vector2(randomX, randomY);
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
            ECSManager.AddComponent<InputComponent>(entityID, new InputComponent(brain.inputs,brain.InputsCount));
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

            string file = $"{filepath}{generation}.{filetype}";
            GeneticAlgorithmDataBatchHandler.SaveBatch(data, file);
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
                if (scav.hasEaten)
                {
                    count++;
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
                // Brain brain =;
                brains[i] = new Brain(info.brainStructure);
                // brain.CopyStructureFrom(info.brainStructure);
                brains[i].SetWeights(newGenomes[i].genome);
            }
        }

        private void RestoreSave()
        {
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


        public void Save()
        {
        }

        public void Load()
        {
            data = GeneticAlgorithmDataBatchHandler.LoadBatch(fileToLoad);
            RestoreSave();
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
        }

        public Herbivore GetNearHerbivore(Vector2 position)
        {
            Herbivore nearest = herbis[0];
            float distance = (position.X * nearest.position.X) + (position.Y * nearest.position.Y);

            foreach (Herbivore go in herbis)
            {
                float newDist = (go.position.X * position.X) + (go.position.Y * position.Y);
                if (newDist < distance)
                {
                    nearest = go;
                    distance = newDist;
                }
            }

            return nearest;
        }

        public virtual Plant GetNearPlant(Vector2 position)
        {
            Plant nearest = plants[0];
            float distance = (position.X * nearest.position.X) + (position.Y * nearest.position.Y);

            foreach (Plant go in plants)
            {
                if (go.isAvailable)
                {
                    float newDist = (go.position.X * position.X) + (go.position.Y * position.Y);
                    if (newDist < distance)
                    {
                        nearest = go;
                        distance = newDist;
                    }
                }
            }

            return nearest;
        }

        public List<Scavenger> GetNearScavs(Vector2 position)
        {
            var nearbyScav = new List<(Scavenger scav, float distance)>();

            foreach (Scavenger go in scavengers)
            {
                float distance = (position.X - go.position.X) * (position.X - go.position.X)
                                 + (position.Y - go.position.Y) * (position.Y - go.position.Y);
                nearbyScav.Add((go, distance));
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
                float distance = (position.X - go.position.X) * (position.X - go.position.X)
                                 + (position.Y - go.position.Y) * (position.Y - go.position.Y);
                nearCarn.Add((go, distance));
            }

            nearCarn.Sort((a, b) => a.distance.CompareTo(b.distance));

            List<Vector2> carnToReturn = new List<Vector2>();
            carnToReturn.Add(nearCarn[0].scav.position);
            carnToReturn.Add(nearCarn[1].scav.position);
            carnToReturn.Add(nearCarn[2].scav.position);
            return carnToReturn;
        }
    }
}