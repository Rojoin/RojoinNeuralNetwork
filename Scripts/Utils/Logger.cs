using System;

namespace RojoinNeuralNetwork.Utils
{
    public static class Logger
    {
        public static event Action<string> LogAction;

        public static void Log(string message)
        {
            LogAction?.Invoke(message);
        }
    }
}