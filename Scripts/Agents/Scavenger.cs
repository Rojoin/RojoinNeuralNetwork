using System;
using System.Collections.Generic;
using System.Numerics;

namespace RojoinNeuralNetwork.Scripts.Agents
{
    public enum ScavengerStates
    {
        Move
    }

    public enum ScavengerFlags
    {
        ToMove
    }

    public sealed class ScavengerMoveState : SporeMoveState
    {
        private float MinEatRadius;
        private int counter;
        private Vector2 dir;
        private float speed;
        private float radius;
        private Brain flockingBrain;
        Vector2 Aligment ;
        Vector2 Cohesion  ;
        Vector2 Separation;
        public override BehaviourActions GetTickBehaviours(params object[] parameters)
        {
            BehaviourActions behaviour = new BehaviourActions();

            float[] outputs = parameters[0] as float[];
            position = (Vector2)(parameters[1]);
            Vector2 nearFoodPos = (Vector2)parameters[2];
            MinEatRadius = (float)(parameters[3]);
            bool hasEatenFood = (bool)parameters[4];
            Herbivore herbivore = parameters[5] as Herbivore;
            var onMove = parameters[6] as Action<Vector2>;
            counter = (int)parameters[7];
            var onEat = parameters[8] as Action<int>;
            dir = (Vector2)parameters[9];
            float rotation = (float)(parameters[10]);
            speed = (float)(parameters[11]);
            radius = (float)(parameters[12]);
            List<Scavenger> nearScavengers = parameters[13] as List<Scavenger>;
            float deltaTime = (float)parameters[14];
            behaviour.AddMultiThreadBehaviour(0, () =>
            {
                List<Vector2> newPositions = new List<Vector2> { nearFoodPos };
                float distanceFromFood = GetDistanceFrom(newPositions);

                if (distanceFromFood < MinEatRadius && !hasEatenFood)
                {
                    counter++;
                    onEat.Invoke(counter);
                    brain.FitnessReward += 1;

                    if (counter >= 20)
                    {
                        brain.FitnessReward += 20;
                        brain.FitnessMultiplier += 0.10f;
                        hasEatenFood = true;
                    }
                }
                else if (distanceFromFood > MinEatRadius)
                {
                    brain.FitnessMultiplier -= 0.05f;
                }

                float leftValue = outputs[0];
                float rightValue = outputs[1];

                float netRotationValue = leftValue - rightValue;
                float turnAngle = netRotationValue * MathF.PI / 180;

                var rotationMatrix = new Matrix3x2(
                    MathF.Cos(turnAngle), MathF.Sin(turnAngle),
                    -MathF.Sin(turnAngle), MathF.Cos(turnAngle),
                    0, 0
                );

                dir = Vector2.Transform(dir, rotationMatrix);
                dir = Vector2.Normalize(dir);
                rotation += netRotationValue;

                rotation = (rotation + 360) % 360;
            });

            behaviour.AddMultiThreadBehaviour(1, () =>
            {
                Aligment = new Vector2(flockingBrain.outputs[0], flockingBrain.outputs[1]);
                Aligment = Vector2.Normalize(Aligment);
                Cohesion  = new Vector2(flockingBrain.outputs[2], flockingBrain.outputs[3]);
                Cohesion = Vector2.Normalize(Cohesion);
                Separation = new Vector2(flockingBrain.outputs[4], flockingBrain.outputs[5]);
                Separation = Vector2.Normalize(Separation);
                float aligmentWeight = 2;
                float cohesionWeight = 2;
                float separationWeight = 2;
                Vector2 flokingInfluence = Aligment * aligmentWeight + Cohesion * cohesionWeight +
                                           Separation * separationWeight + dir;


                Vector2 finalDirection = Vector2.Normalize(flokingInfluence);
                onMove.Invoke(finalDirection);
                position += finalDirection * speed * deltaTime;

        
                if (AlignmentCalc(nearScavengers) != Aligment)
                {
                    flockingBrain.FitnessMultiplier -= 0.05f;
                }
                else
                {
                    flockingBrain.FitnessReward += 1;
                }

                //Cohesion
                if (CohesionCalc(nearScavengers) != Cohesion)
                {
                    flockingBrain.FitnessMultiplier -= 0.05f;
                }
                else
                {
                    flockingBrain.FitnessReward += 1;
                }

                //Separation
                if (SeparationCalc(nearScavengers) != Separation)
                {
                    flockingBrain.FitnessMultiplier -= 0.05f;
                }
                else
                {
                    flockingBrain.FitnessReward += 1;
                }
            });
            

            return behaviour;
        }

        public override BehaviourActions GetEnterBehaviours(params object[] parameters)
        {
            brain = parameters[0] as Brain;
            position = (Vector2)(parameters[1]);
            MinEatRadius = (float)(parameters[2]);
            positiveHalf = Neuron.Sigmoid(0.5f, brain.p);
            negativeHalf = Neuron.Sigmoid(-0.5f, brain.p);
            flockingBrain = parameters[3] as Brain;
            return default;
        }

        public override BehaviourActions GetExitBehaviours(params object[] parameters)
        {
            return default;
        }
        
        public Vector2 AlignmentCalc(List<Scavenger> scavengers)
        {

            Vector2 avg = Vector2.Zero;
            foreach (Scavenger b in scavengers)
            {
                avg += b.dir * b.speed;
            }
            avg /= scavengers.Count;
            avg = Vector2.Normalize(avg);
            return avg;
        }

        public Vector2 CohesionCalc(List<Scavenger> scavengers)
        {
     
            Vector2 avg = Vector2.Zero;
            foreach (Scavenger b in scavengers)
            {
                avg += b.position;
            }
            avg /= scavengers.Count;
            avg = (avg - position);
            avg = Vector2.Normalize(avg);
            return avg;
        }

        public Vector2 SeparationCalc(List<Scavenger> scavengers)
        {
            Vector2 avg = Vector2.Zero;
            foreach (Scavenger b in scavengers)
            {
                avg += (b.position - position);
            }
            avg /= scavengers.Count;
            avg *= -1;
            avg = Vector2.Normalize(avg);
            return avg;
        }
    }


    public class Scavenger : SporeAgent<ScavengerStates, ScavengerFlags>
    {
        public Brain flockingBrain;
        float minEatRadius;
        public Vector2 dir = new Vector2(1, 1);
        protected Vector2 nearFoodPos;
        protected Herbivore nearHerbivore;
        protected List<Scavenger> nearScav = new List<Scavenger>();
        public bool hasEaten = false;
        public int counterEating = 0;
        public float rotation = 0;
        public float speed = 5;
        protected float radius = 2;

        public void Reset(Vector2 position)
        {
            hasEaten = false;
            this.position = position;
            counterEating = 0;
            fsm.ForceState(ScavengerStates.Move);
        }

        public Scavenger(IManager populationManagerLib, Brain main, Brain flockBrain) : base(populationManagerLib, main)
        {
            flockingBrain = flockBrain;
            minEatRadius = 4f;

            Action<Vector2> setDir;
            Action<int> setEatingCounter;
            fsm.AddBehaviour<ScavengerMoveState>(ScavengerStates.Move,
                onEnterParametes: () => { return new object[] { mainBrain, position, minEatRadius, flockingBrain }; },
                onTickParametes: () =>
                {
                    return new object[15]
                    {
                        mainBrain.outputs, position, nearFoodPos, minEatRadius, hasEaten, nearHerbivore,
                        setDir = MoveTo, counterEating, setEatingCounter = b => counterEating = b, dir, rotation, speed,
                        radius, nearScav, deltaTime
                    };
                });

            fsm.ForceState(ScavengerStates.Move);
        }

        public override void DecideState(float[] outputs)
        {
        }

        public override void PreUpdate(float deltaTime)
        {
            this.deltaTime = deltaTime;
            nearFoodPos = GetNearFoodPos();
            mainBrain.inputs = new[] { position.X, position.Y, minEatRadius, nearFoodPos.X, nearFoodPos.Y };

            nearScav = GetNearScavs();
            nearHerbivore = GetNearHerbivore();
            flockingBrain.inputs = new[]
            {
                position.X, position.Y, nearScav[0].position.X, nearScav[0].position.Y, nearScav[1].position.X,
                nearScav[1].position.Y,
                nearScav[0].rotation, nearScav[1].rotation
            };
        }

        public override void Update(float deltaTime)
        {
            fsm.Tick();
            Move(deltaTime);
        }

        private void Move(float deltaTime)
        {
            position += dir * speed * deltaTime;
            if (position.X > PopulationManagerLib.GetGridX())
            {
                position.X = 0;
            }
            else if (position.X < 0)
            {
                position.X = PopulationManagerLib.GetGridX();
            }

            if (position.Y > PopulationManagerLib.GetGridY())
            {
                position.Y = 0;
            }
            else if (position.Y < 0)
            {
                position.Y = PopulationManagerLib.GetGridY();
            }
        }

        public Vector2 GetNearFoodPos()
        {
            return GetNearHerbivore().position;
        }

        public Herbivore GetNearHerbivore()
        {
            return PopulationManagerLib.GetNearHerbivoreScav(position);
        }

        public List<Scavenger> GetNearScavs()
        {
            return PopulationManagerLib.GetNearScavs(this);
        }

        public override void MoveTo(Vector2 dir)
        {
            this.dir = dir;
        }

        public override void GiveFitnessToMain()
        {
            flockingBrain.ApplyFitness();
            mainBrain.FitnessMultiplier = 1.0f;
            mainBrain.FitnessReward = 0f;
            mainBrain.FitnessReward += flockingBrain.FitnessReward + (hasEaten ? flockingBrain.FitnessReward : 0);
            mainBrain.FitnessMultiplier += flockingBrain.FitnessMultiplier + (hasEaten ? 1 : 0);

            mainBrain.ApplyFitness();
        }
    }
}