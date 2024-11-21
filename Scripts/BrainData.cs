using System;
using System.Collections.Generic;


[System.Serializable]
public class BrainData 
{
    public int InputsCount = 4;
   
    public int OutputsCount = 2;
    public int[] NeuronsCountPerHL;
    public float Bias = 1f;
    public float P = 1f;
    
    public BrainData(int inputsCount, int[] NeuronsCountPerHL, int outputsCount, float bias, float p)
    {
       InputsCount = inputsCount;
       this.NeuronsCountPerHL = NeuronsCountPerHL;
       OutputsCount = outputsCount;
       Bias = bias;
       P = p;
    } 
    public Brain ToBrain()
    {
        return Brain.CreateBrain(InputsCount,NeuronsCountPerHL ,OutputsCount, Bias, P);
    }
}