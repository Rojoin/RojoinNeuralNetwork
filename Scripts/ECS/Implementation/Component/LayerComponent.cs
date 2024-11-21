public abstract class LayerComponent : ECSComponent
{
}

public class Layer
{
    public int neuronCount;
    public float[,] weights;

    public Layer(int neuronCount, float[,] weights)
    {
        this.neuronCount = neuronCount;
        this.weights = weights;
    }   
    public Layer(int neuronCount)
    {
        this.neuronCount = neuronCount;
    }
}

public class InputLayerComponent : LayerComponent
{
    public Layer layer;
    public int inputCount;
    public InputLayerComponent(Layer layer)
    {
        this.layer = layer;
    }
}

public class HiddenLayerComponent : LayerComponent
{
    public Layer[] hiddenLayers;
    public int HiggestLayerSize = 0;

    public HiddenLayerComponent(Layer[] hiddenLayers)
    {
        this.hiddenLayers = hiddenLayers;
        SetHighestLayerSize();
    }

    public void SetHighestLayerSize()
    {
        foreach (var layer in this.hiddenLayers)
        {
            if (layer.neuronCount < HiggestLayerSize)
            {
                HiggestLayerSize = layer.neuronCount;
            }
        }
    }
}

public class OutputLayerComponent : LayerComponent
{
    public Layer layer;

    public OutputLayerComponent(Layer layer)
    {
        this.layer = layer;
    }
}

public class OutputComponent : ECSComponent
{
    public float[] outputs;

    public OutputComponent(float[] outputs)
    {
        this.outputs = outputs;
    }
}
public class InputComponent : ECSComponent
{
    public float[] inputs;
    public int size;

    public InputComponent(float[] inputs, int size)
    {
        this.inputs = inputs;
        this.size = size;
    }
}