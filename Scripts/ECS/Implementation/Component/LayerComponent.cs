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

    public HiddenLayerComponent(Layer[] hiddenLayers)
    {
        this.hiddenLayers = hiddenLayers;
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

    public InputComponent(float[] inputs)
    {
        this.inputs = inputs;
    }
}