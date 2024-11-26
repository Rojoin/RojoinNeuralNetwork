﻿using System;
using System.Collections.Generic;
using RojoinNeuralNetwork.Utils;
using Vector2 = System.Numerics.Vector2;

namespace RojoinNeuralNetwork.Scripts.Agents
{
    public enum CarnivoreStates
    {
        Move,
        Eat,
        Escape,
        Dead,
        Corpse
    }

    public enum CarnivoreFlags
    {
        ToMove,
        ToEat,
        ToEscape,
        ToDead,
        ToCorpse
    }

    public class CarnivoreMoveState : SporeMoveState
    {
        private int movesPerTurn = 2;
        private float previousDistance;

        public override BehaviourActions GetTickBehaviours(params object[] parameters)
        {
            BehaviourActions behaviour = new BehaviourActions();

            float[] outputs = parameters[0] as float[];
            position = (Vector2)parameters[1];
            Vector2 nearFoodPos = (Vector2)parameters[2];
            Action<Vector2> onMove = parameters[3] as Action<Vector2>;
            Herbivore herbivore = parameters[4] as Herbivore;
            int gridX = (int)parameters[5];
            int gridY = (int)parameters[6];
            behaviour.AddMultiThreadBehaviour(0, () =>
            {
                if (position == nearFoodPos)
                {
                    herbivore.ReceiveDamage();
                }

                Vector2[] direction = new Vector2[movesPerTurn];
                for (int i = 0; i < direction.Length; i++)
                {
                    direction[i] = GetDir(outputs[i]);
                }

                foreach (Vector2 dir in direction)
                {
                    onMove?.Invoke(dir);
                    position += dir;
                    if (position.X > gridX)
                    {
                        position.X = 0;
                    }
                    else if (position.X < 0)
                    {
                        position.X = gridX;
                    }

                    if (position.Y > gridY)
                    {
                        position.Y = 0;
                    }
                    else if (position.Y < 0)
                    {
                        position.Y = gridY;
                    }
                }

                List<Vector2> newPositions = new List<Vector2> { nearFoodPos };
                float distanceFromFood = GetDistanceFrom(newPositions);
                if (distanceFromFood <= previousDistance)
                {
                    brain.FitnessReward += 20;
                    brain.FitnessMultiplier += 0.05f;
                }
                else
                {
                    brain.FitnessMultiplier -= 0.05f;
                }

                previousDistance = distanceFromFood;
            });
            return behaviour;
        }

        public override BehaviourActions GetEnterBehaviours(params object[] parameters)
        {
            brain = parameters[0] as Brain;
            positiveHalf = Neuron.Sigmoid(0.5f, brain.p);
            Logger.Log($"Positive:{positiveHalf}");
            negativeHalf = Neuron.Sigmoid(-0.5f, brain.p);
            Logger.Log($"Negative:{negativeHalf}");
            return default;
        }

        public override BehaviourActions GetExitBehaviours(params object[] parameters)
        {
            return default;
        }
    }

    public class CarnivoreEatState : SporeEatState
    {
        public override BehaviourActions GetTickBehaviours(params object[] parameters)
        {
            BehaviourActions behaviour = new BehaviourActions();

            float[] outputs = parameters[0] as float[];
            position = (Vector2)parameters[1];
            Vector2 nearFoodPos = (Vector2)parameters[2];
            bool hasEatenEnoughFood = (bool)parameters[3];
            int counterEating = (int)parameters[4];
            int maxEating = (int)parameters[5];
            var onHasEatenEnoughFood = parameters[6] as Action<bool>;
            var onEaten = parameters[7] as Action<int>;
            Herbivore herbivore = parameters[8] as Herbivore;
            behaviour.AddMultiThreadBehaviour(0, () =>
            {
                if (herbivore == null)
                {
                    return;
                }

                if (outputs[0] >= 0f)
                {
                    if (position == nearFoodPos && !hasEatenEnoughFood)
                    {
                        if (herbivore.CanBeEaten())
                        {
                            herbivore.EatBody();
                            //Fitness ++
                            onEaten(++counterEating);
                            brain.FitnessReward += 20;
                            Logger.Log($"Carnivore-{this.GetHashCode()}Has eaten {counterEating} food");
                            if (counterEating >= maxEating)
                            {
                                brain.FitnessReward += 30;
                                Logger.Log($"Carnivore-{this.GetHashCode()}Has eaten enough food");
                                onHasEatenEnoughFood.Invoke(true);
                            }
                            //If comi 5
                            // fitness skyrocket
                        }
                    }
                    else if (hasEatenEnoughFood || position != nearFoodPos)
                    {
                        brain.FitnessMultiplier -= 0.05f;
                    }
                }
                else
                {
                    if (position == nearFoodPos && !hasEatenEnoughFood)
                    {
                        brain.FitnessMultiplier -= 0.05f;
                    }
                    else if (hasEatenEnoughFood)
                    {
                        brain.FitnessMultiplier += 0.10f;
                    }
                }
            });
            return behaviour;
        }

        public override BehaviourActions GetEnterBehaviours(params object[] parameters)
        {
            brain = parameters[0] as Brain;
            return default;
        }

        public override BehaviourActions GetExitBehaviours(params object[] parameters)
        {
            return default;
        }
    }

    public class Carnivore : SporeAgent<CarnivoreStates, CarnivoreFlags>
    {
        public Brain moveBrain;
        public Brain eatBrain;
        int counterEating = 0;
        Vector2 nearestFoodPosition;
        Herbivore herbivore;
        int maxEating = 1;
        public bool hasEatenEnoughFood = false;


        public Carnivore(IManager populationManagerLib, Brain main, Brain move, Brain eat) : base(populationManagerLib,
            main)
        {
            moveBrain = move;
            eatBrain = eat;
            Action<bool> onHasEantenEnoughFood;
            Action<Vector2> onMove;
            Action<int> onEaten;
            fsm.AddBehaviour<CarnivoreEatState>(CarnivoreStates.Eat,
                onEnterParametes: () => { return new object[] { eatBrain }; }, onTickParametes: () =>
                {
                    return new object[]
                    {
                        eatBrain.outputs, position, nearestFoodPosition,
                        hasEatenEnoughFood, counterEating, maxEating,
                        onHasEantenEnoughFood = b =>
                            hasEatenEnoughFood = b,
                        onEaten = i => counterEating = i,
                        herbivore
                    };
                });
            fsm.AddBehaviour<CarnivoreMoveState>(CarnivoreStates.Move,
                onEnterParametes: () => { return new object[] { eatBrain }; }, onTickParametes: () =>
                {
                    return new object[]
                    {
                        moveBrain.outputs, position, nearestFoodPosition,
                        onMove = MoveTo,
                        herbivore,populationManagerLib.GetGridX(),populationManagerLib.GetGridY()
                    };
                });
            fsm.SetTransition(CarnivoreStates.Eat, CarnivoreFlags.ToMove, CarnivoreStates.Move);
            fsm.SetTransition(CarnivoreStates.Move, CarnivoreFlags.ToEat, CarnivoreStates.Eat);
            fsm.ForceState(CarnivoreStates.Move);
        }

        public void Reset(Vector2 position)
        {
            mainBrain.FitnessMultiplier = 1.0f;
            mainBrain.FitnessReward = 0f;
            eatBrain.FitnessMultiplier = 1.0f;
            eatBrain.FitnessReward = 0f;
            moveBrain.FitnessMultiplier = 1.0f;
            moveBrain.FitnessReward = 0f;

            counterEating = 0;
            hasEatenEnoughFood = false;
            this.position = position;
            fsm.ForceState(CarnivoreStates.Move);
        }

        public override void DecideState(float[] outputs)
        {
            if (outputs[0] > 0.0f)
            {
                fsm.Transition(CarnivoreFlags.ToMove);
            }
            else if (outputs[1] > 0.0f)
            {
                fsm.Transition(CarnivoreFlags.ToEat);
            }
        }

        public override void PreUpdate(float deltaTime)
        {
            nearestFoodPosition = GetNearFoodPos();
            herbivore = GetNearHerbivore();

            mainBrain.inputs = new[]
                { position.X, position.Y, nearestFoodPosition.X, nearestFoodPosition.Y, hasEatenEnoughFood ? 1 : -1, };
            moveBrain.inputs = new[] { position.X, position.Y, nearestFoodPosition.X, nearestFoodPosition.Y };
            eatBrain.inputs = new[]
                { position.X, position.Y, nearestFoodPosition.X, nearestFoodPosition.Y, hasEatenEnoughFood ? 1 : -1 };
        }

        public override void Update(float deltaTime)
        {
            DecideState(mainBrain.outputs);
            fsm.Tick();
        }

        public Vector2 GetNearFoodPos()
        {
            return PopulationManagerLib.GetNearHerbivore(position).position;
        }

        public Herbivore GetNearHerbivore()
        {
            return PopulationManagerLib.GetNearHerbivoreCarnivore(position);
        }

        public override void MoveTo(Vector2 dir)
        {
            position += dir;
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

        public override void GiveFitnessToMain()
        {
            moveBrain.ApplyFitness();
            eatBrain.ApplyFitness();
            
            mainBrain.FitnessMultiplier = 1.0f;
            mainBrain.FitnessReward = 0f;
            mainBrain.FitnessReward = eatBrain.FitnessReward + moveBrain.FitnessReward;
            mainBrain.FitnessMultiplier += eatBrain.FitnessMultiplier + moveBrain.FitnessMultiplier;
        }
    }
}