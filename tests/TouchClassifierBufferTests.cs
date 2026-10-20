// Standalone regression harness. Stubs replace only model/Unity dependencies;
// the production TouchClassifier.cs is compiled and exercised unchanged.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine {
    public class TextAsset { public byte[] bytes = new byte[0]; }
    public static class Resources {
        public static T Load<T>(string path) where T : new() { return new T(); }
    }
}
namespace Syncfusion.PMML {
    public class PredictedResult {
        public Dictionary<string, double> GetPredictedProbabilities() {
            return new Dictionary<string, double> { { "1", PMMLEvaluator.Probability } };
        }
    }
    public class PMMLEvaluator {
        public static double Probability;
        public PredictedResult GetResult(object data, object options) { return new PredictedResult(); }
    }
    public class PMMLEvaluatorFactory {
        public PMMLEvaluator GetPMMLEvaluatorInstance(System.IO.Stream stream) { return new PMMLEvaluator(); }
    }
}
public static class TouchClassifierBufferTests {
    static bool Sample(TouchClassifier classifier, int finger, double probability) {
        Syncfusion.PMML.PMMLEvaluator.Probability = probability;
        return classifier.Predict(finger, 0, 0, 0, 0);
    }
    static void Check(bool condition, string message) {
        if (!condition) throw new Exception(message);
    }
    static List<int>[] Windows(TouchClassifier classifier) {
        return (List<int>[])typeof(TouchClassifier).GetField("runningWindow", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(classifier);
    }
    public static string Run() {
        var c = new TouchClassifier();
        Check(!Sample(c, 0, 1), "First positive must not qualify");
        Check(!Sample(c, 0, 1), "Second positive must not qualify");
        Check(Sample(c, 0, 1), "Third positive must qualify");
        Check(Sample(c, 0, 0), "One negative retains three positive votes");
        Check(Sample(c, 0, 0), "Two negatives retain three positive votes");
        Check(!Sample(c, 0, 0), "Third negative must evict the majority");
        Check(!Sample(c, 1, 1), "Finger histories must be independent");
        Check(Windows(c)[0].Count == 5 && Windows(c)[1].Count == 1, "Per-finger window sizes");

        c = new TouchClassifier();
        for (int cycle = 0; cycle < 10; cycle++) {
            Check(!Sample(c, 2, 1), "Separated positives must not accumulate forever");
            for (int n = 0; n < 5; n++) Check(!Sample(c, 2, 0), "Negative sequence must stay false");
        }
        for (int n = 0; n < 10000; n++) Sample(c, 2, n % 2);
        Check(Windows(c)[2].Count == 5, "Long-running history must stay bounded");
        c = new TouchClassifier();
        for (int n = 0; n < 10; n++) Check(!Sample(c, 0, (double)0.3f), "Threshold equality must stay negative");
        for (int n = 0; n < 10; n++) Check(!Sample(c, 4, 1), "Existing pinky confidence threshold must be preserved");
        return "PASS: warm-up, majority, negative eviction, finger isolation, separated contacts, bounded history, confidence boundaries";
    }
}
