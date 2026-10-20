using UnityEngine;
using Vuplex.WebView;
using UnityEngine.XR;
using System.IO;
using System;

class OculusWebViewDemo : MonoBehaviour {
    public enum Tasks
    {
        None,
        Scroll,
        Docking,
        Google,
        Sketching,
        NationalParkOverview,
        ListOfPark,
        YellowStonePark,
        Map,
        Quiz,
        Canvas,
        Storage1,
        Storage2,
        Tutorial1,
        Tutorial2,
        Paper,
        Wiki,
        WebFitt,
        test1,
        test2,
        test3
    }

    public string IP_Address;
    public string Port;

    public int P_ID;
    public int S_ID;
    public int C_ID;

    private int p_id;
    private int c_id;
    private int s_id;

    public string W_STR = "20.8,33.4,20.8,33.4,20.8,33.4";

    WebViewPrefab _webViewPrefab;
    //Keyboard _keyboard;

    [SerializeField]
    public Tasks task;

    /*[SerializeField]
    public bool screenshot = false;*/

    async void Start() {

        // Optional desktop User-Agent override (disabled below).
        // https://developer.vuplex.com/webview/Web#SetUserAgent
        //Web.SetUserAgent(false);

        // Create a 0.6 x 0.4 instance of the prefab.
        // https://developer.vuplex.com/webview/WebViewPrefab#Instantiate
        _webViewPrefab = WebViewPrefab.Instantiate(0.6f, 0.4f);
        _webViewPrefab.transform.SetParent(transform, false);
        _webViewPrefab.transform.localPosition = new Vector3(0, 0.2f, 0.6f);
        _webViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);

        // Optional on-screen keyboard example (disabled below).
        // https://developer.vuplex.com/webview/Keyboard
        /*_keyboard = Keyboard.Instantiate();
        _keyboard.transform.SetParent(_webViewPrefab.transform, false);
        _keyboard.transform.localPosition = new Vector3(0, -0.41f, 0);
        _keyboard.transform.localEulerAngles = Vector3.zero;*/

        // Wait for the prefab to initialize because its WebView property is null until then.
        // https://developer.vuplex.com/webview/WebViewPrefab#WaitUntilInitialized
        await _webViewPrefab.WaitUntilInitialized();

        // After the prefab has initialized, you can use the IWebView APIs via its WebView property.
        // https://developer.vuplex.com/webview/IWebView
        
        
        if (task == Tasks.None)
        {
            ;
        }
        else if (task == Tasks.Scroll)
            _webViewPrefab.WebView.LoadUrl("http://localhost:5000/scrollingtask?participant_id=P01&target_position=-0.5&threshold=0.8&target_thickness=100&scroll_limit=5000&success_time_threshold=500&drag_enabled=true&scroll_enabled=true&inverted_scroll=false");
        else if (task == Tasks.Docking)
        {
            
            //string webLink = "http://" + IP_Address + ":" + Port + "/dockingtask?participant_id=" + P_ID.ToString() + "&source_position_x=0.2&source_position_y=0.2&source_size=100&source_orientation=0&target_position_x=0.8&target_position_y=0.7&target_size=250&target_orientation=90&distance_threshold=20&rotation_threshold=10&scale_threshold=10&dwell_time_threshold=500";
            

            string webLink = "http://" + IP_Address + ":" + Port + "/dockingtask?participant_id=" + P_ID.ToString() + "&condition_id=" + C_ID.ToString();
            _webViewPrefab.WebView.LoadUrl(webLink);

            p_id = P_ID;
            c_id = C_ID;
            s_id = S_ID;

        }
        else if (task == Tasks.Google)
        {
            
            _webViewPrefab.WebView.LoadUrl("https://en.wikipedia.org/wiki/List_of_national_parks_of_the_United_States");
        }
        else if (task == Tasks.Sketching)
        {
            _webViewPrefab.WebView.LoadUrl("http://localhost:5002/");
        }
        else if (task == Tasks.NationalParkOverview)
        {
            
            _webViewPrefab.WebView.LoadUrl("http://127.0.0.1:5000/page/nationalpark");
        }
        else if (task == Tasks.YellowStonePark)
        {
            _webViewPrefab.WebView.LoadUrl("http://127.0.0.1:5000/page/yellowstonepark");
        }
        else if (task == Tasks.ListOfPark)
        {
            
            _webViewPrefab.WebView.LoadUrl("http://127.0.0.1:5000/page/listofnationalpark");
        }
        else if (task == Tasks.Map)
        {
            
            _webViewPrefab.WebView.LoadUrl("http://127.0.0.1:5000/map");
        }
        else if (task == Tasks.Quiz)
        {
            _webViewPrefab.WebView.LoadUrl("http://127.0.0.1:5000/quiz");
        }
        else if (task == Tasks.Canvas)
        {
            _webViewPrefab.WebView.LoadUrl("http://127.0.0.1:5000/canvas2");
        }
        else if (task == Tasks.Tutorial1)
        {
            _webViewPrefab.WebView.LoadUrl("http://127.0.0.1:5000/tutorial");
        }
        else if (task == Tasks.Tutorial2)
        {
            _webViewPrefab.WebView.LoadUrl("http://127.0.0.1:5000/page/tutorial2");
        }
        else if (task == Tasks.Paper)
        {
            _webViewPrefab.WebView.LoadUrl("https://www.dgp.toronto.edu/OTP/papers/bill.buxton/3state.html");
        }
        else if (task == Tasks.test1)
        {
            _webViewPrefab.WebView.LoadUrl("about:blank");
        }
        else if (task == Tasks.test2)
        {
            _webViewPrefab.WebView.LoadUrl("https://en.wikipedia.org/wiki/Knowledge_worker");
        }
        else if (task == Tasks.test3)
        {
            _webViewPrefab.WebView.LoadUrl("about:blank");
        }
        else if (task == Tasks.Paper)
        {
            _webViewPrefab.WebView.LoadUrl("https://www.dgp.toronto.edu/OTP/papers/bill.buxton/3state.html");
        }
        else if (task == Tasks.Paper)
        {
            _webViewPrefab.WebView.LoadUrl("https://www.dgp.toronto.edu/OTP/papers/bill.buxton/3state.html");
        }
        else if (task == Tasks.Wiki)
        {
            _webViewPrefab.WebView.LoadUrl("https://en.wikipedia.org/wiki/List_of_national_parks_of_the_United_States");
        }
        else if (task == Tasks.WebFitt)
        {


            string webLink = "http://" + IP_Address + ":" + Port + "/customtask?p_id=" + P_ID.ToString() + "&s_id=" + S_ID.ToString() + "&c_id=" + C_ID.ToString() + "&a=500&w=" + W_STR + "&n=11&scale=1";
            _webViewPrefab.WebView.LoadUrl(webLink);

            p_id = P_ID;
            c_id = C_ID;
            s_id = S_ID;
        }
    }

    /*async void Upate()
    {
        if (screenshot)
        {
            // Get a screenshot and write it to a file.
            var screenshotBytes = await _webViewPrefab.WebView.CaptureScreenshot();
            //var filePath = Path.Combine(Application.persistentDataPath, "screenshot.png");
            string filePath = Application.dataPath + "/../Data/" + "screenshot_" + string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) + ".png";
            File.WriteAllBytes(filePath, screenshotBytes);

            screenshot = false;
        }
    }*/

    











}
