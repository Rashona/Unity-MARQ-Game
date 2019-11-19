using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// serves as the constant class that controls the camera scene, primarily incharge of validating QR codes
public class CameraControl : MonoBehaviour {

    public static CameraControl control;
    Hashtable map; //maps image name to the badge
    public GameObject textObj, imgObj, ssGrp;

    // enforce singleton
    void Awake()
    {
        if (control == null)
        { // if this instance is the first
            control = this;
        }
        else if (control != this)
        { // if object is not the one destroy it
            Destroy(gameObject);
        }

        //get badge data
        string badgeData = JsonHelper.getFileString("badges.txt");
        string[] data = badgeData.Split('\n');
        map = new Hashtable();
        parseToMap(data);
        Debug.Log("Created Camera controller");
        Debug.Assert(ssGrp.name == "ss text bg");
        ssGrp.SetActive(false);
    }
    

    // helper to init the map from target name to badge name
    private void parseToMap(string[] data)
    {
        int cnt = 0;
        foreach (string line in data)
        {
            string[] split = line.Split(',');
            string name = split[1];
            cnt++;
            if (cnt != data.Length)
            {
                name = name.Remove(name.Length - 1);
            }
            map.Add(split[0], name);
        }
    }

    // specific to answering QR questions
    public bool validateAnswer(string input)
    {
        string answer = GameControl.control.getCurrAnswer();
        answer = answer.ToLower();
        if (answer == input)
        {
            return true;
        }
        return false;
    }

    IEnumerator fadeMessage()
    {
        // fade
        float seconds = 8;
        //Wait for 4 seconds
        yield return new WaitForSeconds(seconds);

        ssGrp.SetActive(false);
    }

    // give user feedback based on result of QR scan
    public void giveFeedback(string message)
    {
        ssGrp.SetActive(true);
        // load whever current super searcher is to deliver message
        imgObj.GetComponent<Image>().enabled = true;
        ssGrp.GetComponent<Image>().enabled = true;
        imgObj.GetComponent<Image>().sprite = Resources.Load<Sprite>(
            "CharImages/" + GameControl.control.getNextEvent().image);      //changed "GameControl.control.getCurrEvent().image" to "GameControl.control.getNextEvent().image"
        // set text to message
        textObj.GetComponent<TextMeshProUGUI>().SetText(message);
        StartCoroutine(fadeMessage());
    }

    // return index of the most recent QR question
    private int getQRQuestion()
    {
        int i = GameControl.control.getIndex();
        while(GameControl.control.getEvent(i).type != "qr question")
        {
            ++i;
        }
        Debug.Log(i.ToString() + " : " + GameControl.control.getEvent(i).text);
        return i;
    }

    // called when an image is foundF
    public void handleScan(string input)
    {

        // if this is the correct answer
        if (map.ContainsKey(input))
        {
            
            string badgename = map[input].ToString();
            Debug.Log("badgename:" + badgename);
            if (!GameControl.control.hasBadge(badgename))
            {
                // add badge
                GameControl.control.addBadge(badgename);    //this adds any recognized badges into the badge book
                // give message
                string msg = "You've scanned a unprompted QR code. "+" This will be an important QR code later, but for now please return to the main menu.";
                giveFeedback(msg);
            }
            if (GameControl.control.getCurrAnswer() != null)
            {
                if (validateAnswer(input))
                {
                    // get the qr question event
                    int index = getQRQuestion();
                    Debug.Log("Set index to: " + index);
                    Debug.Log(GameControl.control.getEvent(index).answer + " : " + input);
                    Debug.Assert(GameControl.control.getEvent(index).answer == input);
                    giveFeedback(GameControl.control.getEvent(index + 1).text);
                    // handle correct answer
                    GameControl.control.handleQRAnswer();
                }
            }

        }
        else
        {
            giveFeedback(GameControl.control.getCurrEvent().wrong);
        }
    }
}
