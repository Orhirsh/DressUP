using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;

public class AppManager : MonoBehaviour
{
    private string apiKey = "3652237f85b04cd290f35d76597773c0";
    private string apiURL = "https://api.bing.microsoft.com/v7.0/images/search";
    [SerializeField] TMP_Dropdown genderDropdown;
    [SerializeField] TMP_Dropdown formalityDropdown;
    [SerializeField] TextMeshProUGUI title; 
    private Image backgroundPic;
    public Image topImage;
    public Image pantsImage;
    public Image shoesImage;
    public Button getOutfitButton;
    private HashSet<string> usedUrls = new HashSet<string>();

   

    void Start()
    {
        // Get references to the gender and formality dropdowns
        genderDropdown = GameObject.Find("GenderDropdown").GetComponent<TMP_Dropdown>();
        formalityDropdown = GameObject.Find("FormalityDropdown").GetComponent<TMP_Dropdown>();
        backgroundPic = GameObject.Find("BackgroundPic").GetComponent<Image>();
        title = GameObject.Find("Title").GetComponent<TextMeshProUGUI>();


        // Get outfit recommendation when the button is clicked
        getOutfitButton.onClick.AddListener(GetOutfitRecommendation);
            // Add OnClick listener to the title
        title.GetComponent<BoxCollider2D>().gameObject.AddComponent<Button>();
        title.GetComponent<BoxCollider2D>().gameObject.GetComponent<Button>().onClick.AddListener(ResetApp);
    }

    public void GetOutfitRecommendation()
    {
        
        // Get the selected values from the gender and formality dropdowns
        string gender = genderDropdown.options[genderDropdown.value].text;
        string formality = formalityDropdown.options[formalityDropdown.value].text;

        // Construct the search qu eries
        string shoesQuery =  gender + " " + formality + " shoes";
        string shirtQuery =  gender + " " + formality +" shirt";
        string pantsQuery =  gender + " " + formality +" pants";

        // Generate random offsets for each search query
        int shoesOffset = Random.Range(0, 51);
        int shirtOffset = Random.Range(0, 51);
        int pantsOffset = Random.Range(0, 51);

        // Construct the full API URLs with query parameters
        string shoesURL = apiURL + "?q=" + UnityWebRequest.EscapeURL(shoesQuery) + "&count=1&offset= " + shoesOffset;
        string shirtURL = apiURL + "?q=" + UnityWebRequest.EscapeURL(shirtQuery) + "&count=1&offset= " + shirtOffset;
        string pantsURL = apiURL + "?q=" + UnityWebRequest.EscapeURL(pantsQuery) + "&count=1&offset= " + pantsOffset;

        //Set background pic alpha for better visability
        backgroundPic.color = new Color(backgroundPic.color.r, backgroundPic.color.g, backgroundPic.color.b, 0.2f);

        StartCoroutine(LoadImage(shoesURL, shoesImage));
        usedUrls.Add(shoesURL);

        // Load the shirt image (with delay to avoid exceeding API limits)
        StartCoroutine(WaitAndLoadImage(2f, shirtURL, topImage));
        usedUrls.Add(shirtURL);

        // Load the pants image (with delay to avoid exceeding API limits)
        StartCoroutine(WaitAndLoadImage(2.5f, pantsURL, pantsImage));
        usedUrls.Add(pantsURL);
    }
    //creating a delay since I can make only 3 calls persecond
    IEnumerator WaitAndLoadImage(float delay, string url, Image image)
    {
        yield return new WaitForSeconds(delay);

        // If URL has already been used, generate new offset and URL
        while (usedUrls.Contains(url))
        {
            int offset = Random.Range(0, 51);
            url = apiURL + "?q=" + UnityWebRequest.EscapeURL(url.Substring(20)) + "&count=1" + "&offset=" + offset;
        }

        // Load the image
        StartCoroutine(LoadImage(url, image));
        usedUrls.Add(url);
    }

    IEnumerator LoadImage(string url, Image image)
    {
        
        topImage.gameObject.SetActive(true);
        pantsImage.gameObject.SetActive(true);
        shoesImage.gameObject.SetActive(true);

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Add the API key to the request headers
            webRequest.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);

            // Send the request
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + webRequest.error);
                yield break;
            }
            

            // Get the response data
            string response = webRequest.downloadHandler.text;

            // Parse the response data into a JSON object
            var responseData = JsonUtility.FromJson<BingResponse>(response);

            // Get the URL of the first image result
            string imageUrl = responseData.value[0].contentUrl;
            
            

            // Load the image from the URL and assign it to the corresponding Image component
            using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return imageRequest.SendWebRequest();

                if (imageRequest.result == UnityWebRequest.Result.ConnectionError || imageRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error: " + imageRequest.error);
                    yield break;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                Debug.Log(responseData.value[0].contentUrl);
                
            }
        }
    }
    public void ResetApp()
    {
        // Reset dropdowns
        genderDropdown.value = 0;
        formalityDropdown.value = 0;

        // Clear used URLs set
        usedUrls.Clear();

        // Hide images
        topImage.gameObject.SetActive(false);
        pantsImage.gameObject.SetActive(false);
        shoesImage.gameObject.SetActive(false);

        // Reset background pic alpha
        backgroundPic.color = new Color(backgroundPic.color.r, backgroundPic.color.g, backgroundPic.color.b, 1f);
    }

    [System.Serializable]
    public class BingResponse
    {
        public BingImageResult[] value;
    }

    [System.Serializable]
    public class BingImageResult
    {
        public string name;
        public string thumbnailUrl;
        public string contentUrl;
        public string hostPageUrl;
    }
}