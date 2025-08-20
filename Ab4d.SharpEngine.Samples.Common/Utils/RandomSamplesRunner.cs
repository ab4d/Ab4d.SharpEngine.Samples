#define DEBUG // DEBUG is defined to enable debug logging

using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Ab4d.SharpEngine.Samples.Common.Utils;


// To use RandomSamplesRunner in Avalonia sample add the following code to the end of SamplesWindow.xaml.cs file:

// private RandomSamplesRunner? _randomSamplesRunner;
// 
// private void SetupTestButton()
// {
//     var button = new Button() { Content = "TEST" };
//     button.Click += TestButton_OnClick;
//     ButtonsPanel.Children.Insert(0, button);
// }
// 
// private void TestButton_OnClick(object? sender, RoutedEventArgs e)
// {
//     if (_randomSamplesRunner == null)
//     {
//         string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples.xml");
//         var samplesXmlNodList = CommonSample.LoadSamples(fileName, uiFramework: "Avalonia", errorMessage => ShowError(errorMessage));
//         samplesXmlNodList = samplesXmlNodList.Where(n => n.Attributes != null && n.Attributes["Location"] != null).ToList(); // Skip separators because they are not added to SamplesList.Items
//         
//         _randomSamplesRunner = new RandomSamplesRunner(samplesList: samplesXmlNodList,
//                                                        sampleSelectorAction: sampleIndex => SamplesList.SelectedIndex = sampleIndex,
//                                                        beginInvokeAction: action => Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.ApplicationIdle),
//                                                        customLogAction: null,
//                                                        showCurrentlyRunningSample: true); // set to false to speed up showing samples
//     }
//             
//     if (_randomSamplesRunner.IsRunning)
//         _randomSamplesRunner.Stop();
//     else
//         _randomSamplesRunner.Start();
//         // _randomSamplesRunner.Start(startSampleIndex: 10, endSampleIndex: 100); // To get the indexes of the samples run _randomSamplesRunner.DumpAllSamples() in Immediate window
// } 


// To use RandomSamplesRunner in WPF sample add the following code to the end of MainWindow.xaml.cs file:

// private RandomSamplesRunner? _randomSamplesRunner;
//
// private void SetupTestButton()
// {
//     var button = new Button() { Content = "TEST" };
//     button.Click += TestButton_OnClick;
//     ButtonsPanel.Children.Insert(0, button);
// }
//
// private void TestButton_OnClick(object sender, RoutedEventArgs e)
// {
//     if (_randomSamplesRunner == null)
//     {
//         _randomSamplesRunner = new RandomSamplesRunner(samplesList: (List<System.Xml.XmlNode>)SampleList.ItemsSource,
//                                                        sampleSelectorAction: sampleIndex => SampleList.SelectedIndex = sampleIndex,
//                                                        beginInvokeAction: action => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, action),
//                                                        customLogAction: null,
//                                                        showCurrentlyRunningSample: true); // set to false to speed up showing samples
//     }
//     
//     if (_randomSamplesRunner.IsRunning)
//         _randomSamplesRunner.Stop();
//     else
//         _randomSamplesRunner.Start();
//         // _randomSamplesRunner.Start(startSampleIndex: 10, endSampleIndex: 100); // To get the indexes of the samples run _randomSamplesRunner.DumpAllSamples() in Immediate window
// }

public class RandomSamplesRunner
{
    private readonly List<System.Xml.XmlNode> _samplesList;
    private readonly Action<int> _sampleSelectorAction;
    private readonly Action<Action> _beginInvokeAction;
    private readonly Action<string> _logAction;
    
    private Stopwatch? _randomSampleStopwatch;

    private int _startSampleIndex;
    private int _endSampleIndex;
    private bool _showCurrentlyRunningSample;

    public bool IsRunning => StartedSamplesCount > 0;
    
    public int AllSamplesCount => _samplesList.Count;
    
    public int StartedSamplesCount { get; private set; }

    public bool SkipGarbageCollectAfterEachSample { get; set; } = true;
    private bool savedCollectGarbageAfterEachSample;
    
    public RandomSamplesRunner(List<System.Xml.XmlNode> samplesList, 
                               Action<int> sampleSelectorAction, 
                               Action<Action> beginInvokeAction,
                               Action<string>? customLogAction,
                               bool showCurrentlyRunningSample = true) // Set this to false to speed up showing samples (calling Debug.WriteLine takes a lot of time)
    {
        _samplesList = samplesList;
        _sampleSelectorAction = sampleSelectorAction;
        _beginInvokeAction = beginInvokeAction;
        _logAction = customLogAction ?? ((message) => System.Diagnostics.Debug.WriteLine(message));
        _showCurrentlyRunningSample = showCurrentlyRunningSample;
    }
    
    /// <summary>
    /// Run all samples
    /// </summary>
    public void Start()
    {
        Start(0, _samplesList.Count - 1);
    }

    /// <summary>
    /// Run samples from startSampleIndex to endSampleIndex.
    /// </summary>
    /// <param name="startSampleIndex">startSampleIndex</param>
    /// <param name="endSampleIndex">endSampleIndex</param>
    public void Start(int startSampleIndex, int endSampleIndex)
    {
        if (IsRunning || endSampleIndex <= startSampleIndex)
            return;
        
        _startSampleIndex = startSampleIndex;
        _endSampleIndex = endSampleIndex;
        
        StartedSamplesCount = 1;
        _randomSampleStopwatch ??= new Stopwatch();
        _randomSampleStopwatch.Restart();
        
        if (SkipGarbageCollectAfterEachSample)
        {
            savedCollectGarbageAfterEachSample = CommonSample.CollectGarbageAfterEachSample;
            CommonSample.CollectGarbageAfterEachSample = false;
        }
                
        _beginInvokeAction(SelectNewRandomTest);
    }
    
    public void Stop()
    {
        if (!IsRunning)
            return;
        
        if (_randomSampleStopwatch != null)
        {
            _randomSampleStopwatch.Stop();
            
            var seconds = _randomSampleStopwatch.Elapsed.TotalSeconds;
            var startedSamplesCount = StartedSamplesCount - 1; // StartedSamplesCount starts with 1 before the first sample is started

            _logAction($"Opened {startedSamplesCount} samples in {seconds:F2} seconds => {((double)startedSamplesCount/seconds):F1} samples/s or {((seconds*1000)/(double)startedSamplesCount):F2} ms/sample");
        }
                
        StartedSamplesCount = 0; // mark that tests are not running anymore
        
        CommonSample.CollectGarbageAfterEachSample = savedCollectGarbageAfterEachSample;
    }
    
    private void SelectNewRandomTest()
    {
        if (StartedSamplesCount == 0)
            return;

        int selectedIndex;
        XmlAttribute? isTitleAttribute;
        XmlAttribute? locationAttribute;

        do
        {
            selectedIndex = Random.Shared.Next(_endSampleIndex - _startSampleIndex + 1) + _startSampleIndex;

            var xmlNode = _samplesList[selectedIndex]!;

            if (xmlNode.Attributes != null)
            {
                isTitleAttribute = xmlNode.Attributes["IsTitle"];
                locationAttribute = xmlNode.Attributes["Location"];
            }
            else
            {
                isTitleAttribute = null;
                locationAttribute = null;
            }
        }
        while (isTitleAttribute != null || locationAttribute == null); // skip titles and separators

        if (_showCurrentlyRunningSample)
            _logAction($"{StartedSamplesCount}  [{selectedIndex}] {locationAttribute.Value}"); // Write which sample is starting
        
        _sampleSelectorAction(selectedIndex);

        StartedSamplesCount++;
            
        _beginInvokeAction(SelectNewRandomTest);
    }   
    
    public void DumpAllSamples()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < _samplesList.Count; i++)
        {
            var xmlNode = _samplesList[i]!;

            if (xmlNode.Attributes != null)
                sb.Append($"[{i}]  {(xmlNode.Attributes["Location"]?.Value ?? "<null>")}\r\n");
        }
        
        System.Diagnostics.Debug.WriteLine(sb.ToString());
    }
}