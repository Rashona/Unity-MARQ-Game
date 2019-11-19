﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;
using TMPro;
using System.Text.RegularExpressions;

/*
 * This class serves as the game's state saving device. It is a Singleton Object that must be present
 * in every scene. It is used as the parent to perserve main UI. Most events are run throught this.
 * When in other scripts or objects and you need to call something from this object do:
 *      GameControl.control.method()
*/
public class GameControl : MonoBehaviour {


////////////// VARIABLES //////////////
    public static GameControl control; // singleton
    public int team; // team number used for ordering events
    private string flowFilename = "script.json";
    private int clueCount = 0; // number of clues recieved
    GameEventCollection events; // array of events that will occur
    private int index = 0; // index in the array of events
    private string currAnswer = null; // the answer to the current question, null if no answer is expected
    public float minutesClueWait = 2.5f; // number of minutes to wait before giving final anwser clues

    // ui elements that this affects. These objects should be given in Unity UI
    public GameObject repeat, dialogue, nextDialogue, questionPanel, 
                      textInput, qrInput, cluePrompt, badgeCount, badgePanel, bioButtons, displayImage,
                      contentBox, puzzlePanel;
    Image ssImage, nameTag, dispimg; // images used for representing supersearchers

    // flags and variables
    public bool isWrong = false; // if answer given was wrong
    int answerIndex; // index of the question that needs to be answered
    List<string> badges; // list of badges players have collected
    List<string> searchers; // list of super searchers player has met


    ////////////// GET AND SET FUNCTIONS //////////////
    // get index that curr event is 
    public int getIndex() { return index; }
    // get answer to most recent answer
    public string getCurrAnswer() { return currAnswer; }
    // set game index
    public void setIndex(int i) { index = i; }
    // get event at an index
    public GameEvent getEvent(int index) { return events.get(index); }
    // get current event
    public GameEvent getCurrEvent() { return events.get(index); }
    // get next event
    public GameEvent getNextEvent() { return events.get(index + 1); }
    // set dialogue of super searcher speech 
    public void setDialogue(string input)
    {
        dialogue.GetComponent<TextMeshProUGUI>().SetText(input);
    }
    public void setImage(string input)// create image that appears during script
    {
        displayImage.GetComponent<Image>().sprite = Resources.Load<Sprite>("DialogImages/" + input);
    }

////////////// SEARCHER AND BADGE FUNCTIONS //////////////
    // bool to see if player has a badge based on the name. Must match exactly
    public bool hasBadge(string badgeName){ return badges.Contains(badgeName); }
    // add badge to players collection. Should be called from scan of QR
    public void addBadge(string newBadge){
        badges.Add(newBadge);
        Debug.Log("Added " + newBadge + " now has size " + badges.Count);
        // set badge to unlocked
        string badgeToLoad = string.Format("{0}_{1}", newBadge, "badge");
        // get badge to change image for
        foreach (Transform child in badgePanel.transform)
        {
            Debug.Log("comparing: '" + child.name + "' to actual : '" + badgeToLoad + "'");
            if (child.name.Equals(badgeToLoad, StringComparison.Ordinal)){
                child.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Badges/" + badgeToLoad);
                break;
            }
        }
    }
    
    // add a searcher to players collection. Needed to access SS bio
    public void addSearcher(string searcher)
    {
        searchers.Add(searcher);
        // set bio button to unlocked image
        foreach (Transform button in bioButtons.transform)
        {
            if (button.name.Equals(searcher + "_btn")){
                button.GetComponent<Image>().sprite = Resources.Load<Sprite>("CharImages/" + searcher + "_unlocked");
                Debug.Log("unlocked " + searcher);
            }
        }
    }

    // determine if a searcher is unlocked by name
    public bool searcherIsUnlocked(string searcher)
    {
        return searchers.Contains(searcher);
    }

////////////// INITIALIZATION FUNCTION //////////////
    // helper called to grab all needed parts (GameObject) at awake
    private void loadData()
    {
        // assign team
        if (PlayerPrefs.HasKey("team")) { team = PlayerPrefs.GetInt("team"); }
        else // can't get pref, but this might be because of testing 
        {
            //TODO this is hard coded for testing, must be updated for game
            team = 0;
            Debug.Log("Hard coded team to be 0");
        }
        // load event data from file
        switch (team) //selecting team and loading script based on that. 
        {
            case 0:
                team = 0;
                flowFilename = "script.json";
                break;
            case 1:
                team = 1;
                flowFilename = "script2.json";
                break;
            case 2:
                team = 2;
                flowFilename = "script3.json";
                break;
            case 3:
                team = 3;
                flowFilename = "script4.json";
                break;
            default:
                break;                
        }
        events = JsonUtility.FromJson<GameEventCollection>(JsonHelper.getFileString(flowFilename));
        // load reapeat UI btn
        Transform canvas = GameObject.Find("Canvas").transform;
        // get dialogue elements
        Transform ssGrp = canvas.GetChild(1).gameObject.transform;
        Debug.Assert(ssGrp.name == "ss grp");
        ssImage = ssGrp.GetChild(1).gameObject.GetComponent<Image>();
        nameTag = ssImage.transform.GetChild(0).gameObject.GetComponent<Image>();
        dispimg = ssImage.transform.GetChild(1).gameObject.GetComponent<Image>();

        Debug.Assert(nameTag.name == "nameTag");
        // init private variables
        badges = new List<string>();
        searchers = new List<string>();
        // set ui to first event
        setUIElements();
        // init puzzles for clues
        foreach (Transform child in puzzlePanel.transform)
        {
            child.gameObject.GetComponent<puzzlePieceOnClick>().initPuzzlePiece();
        }
    }

    // initialization that enforces singleton and loads data
	void Awake () {
        // enforce singleton
        if (control == null)
        { // if this instance is the first
            Debug.Log("Made the one and only version of GameControl");
            control = this;
            loadData();
        }
        else if (control != this)
        { // if object is not the one destroy it
            Debug.Log("Going to destroy this");
            Destroy(gameObject);
        }        
        DontDestroyOnLoad(gameObject);
    }
////////////// EVENT FUNCTIONS //////////////
    // using the index set the text and image elements 
    public void setUIElements()
    {
        setDialogue(events.get(index).text);
        if (events.get(index).detail_image == null)
        {
            displayImage.SetActive(false);
        }
        else
        {
            displayImage.SetActive(true);
            dispimg.sprite = Resources.Load<Sprite>("DialogImages/" + events.get(index).detail_image);
        }
        // if need to change image
        if (ssImage.sprite.name != events.get(index).image)
        {
            ssImage.sprite = Resources.Load<Sprite>("CharImages/" + events.get(index).image);
            // update name tag
            string name = events.get(index).image.Split('_')[0];
            nameTag.sprite = Resources.Load<Sprite>("CharImages/" + name + "_name");
            if (!searchers.Contains(name))
            {
                addSearcher(name);
            }
        }
        // update badge count
        badgeCount.GetComponent<TMP_Text>().text = badges.Count.ToString() + "/7";
    }


    // used to turn repeat btn off or on. Does inverse of next dialogue button
    public void toggleRepeat()
    {
        Debug.Log(nextDialogue);
        repeat.SetActive(!repeat.activeSelf);
        nextDialogue.SetActive(!nextDialogue.activeSelf);
    }

    // given a text input determine if it is the answer to the current question
    public bool validateAnswer(string input) { return events.get(index).validateAnswer(input); }

    public void handleQRAnswer()
    {
        setIndex(answerIndex+1);
        //index++; // move to next event
        //nextEvent();
        setUIElements();
        toggleRepeat();
        qrInput.SetActive(false);
        contentBox.SetActive(false);
    }

    // this function deals with qr questions and is dealt with inside vuforia's DefaultTrackableEventHandler
    public void handleQRQuestion()
    {
        index = answerIndex;
        setUIElements();
    }

    private IEnumerator giveTimedClue(GameObject hint, float waitTime)
    {
        // wait for event to be unlocked
        yield return new WaitForSeconds(waitTime);
        hint.SetActive(true);
    }
    
    // set up to give a clue when earned
    void handleClue()
    {
        contentBox.SetActive(true);
        cluePrompt.SetActive(true);
        clueCount++;
        // try and show the new puzzle
        foreach(Transform child in puzzlePanel.transform)
        {
            if (child.name.EndsWith(clueCount.ToString()))
            {
                child.gameObject.GetComponent<puzzlePieceOnClick>().unlockPiece();
            }
        }
        Debug.Log("count: " + clueCount.ToString());
        // if they have unlocked every clue give option to answer
        if (clueCount == 3)
        {
            Debug.Log("in if");
            Debug.Log(puzzlePanel.transform.parent.GetChild(1).GetChild(0).GetChild(0).gameObject.name);
            Debug.Log(puzzlePanel.transform.parent.GetChild(1).gameObject.name);
            puzzlePanel.transform.parent.GetChild(1).gameObject.SetActive(true); // set buttons active
            puzzlePanel.transform.parent.GetChild(1).GetChild(0).GetChild(0).gameObject.SetActive(true); // set answer active
            // set to give clues after timer
            StartCoroutine(giveTimedClue(puzzlePanel.transform.parent.GetChild(1).
                           GetChild(0).GetChild(1).gameObject, minutesClueWait * 60));
            StartCoroutine(giveTimedClue(puzzlePanel.transform.parent.GetChild(1).GetChild(0).
                           GetChild(2).gameObject, minutesClueWait * 60*1.5f));
        }
    }

    // when you get to the final question remove next button, only move forward
    // on anwser from puzzle input
    void prepareFinalQuestion()
    {
        Debug.Log("in prepare");
        //toggleRepeat();
    }

    // when team has submitted correct final answer
    public void handleFinalAnswer()
    {
        // move to next event
        setIndex(index + 1);
    }

    // when a question is ready, init and show related elements needed to answer
    public void prepareQuestion()
    {
        // show repeat dialogue option
        repeat.SetActive(true);
        nextDialogue.SetActive(false);
        questionPanel.SetActive(true);
        // show blue content box
        contentBox.SetActive(true);
        currAnswer = events.get(index).answer;
        // show answer boxes according to event
        switch (events.get(index).type)
        {
            // for each event set required element to active
            case "text question":
                Debug.Log("text question");
                textInput.SetActive(true);
                break;
            case "cite question":
                Debug.Log("cite question");
                //TODO create cite handler
                break;
            case "qr question":
                Debug.Log("qr question, index : " + index);
                qrInput.SetActive(true);
                answerIndex = index;
                handleQRQuestion();
                break;
            case "final question":
                Debug.Log("at final question");
                prepareFinalQuestion();
                break;
        }
    }

    // try and move to next event in queue in script
    public void nextEvent()
    {        
        // if a wrong answer was given and then clicked, show question again
        if (isWrong)
        {
            setDialogue(events.get(index).text);
            isWrong = false;
        }
        if (index < 0) { index = 0; }
        else if (events.get(index).type == "dialogue" || events.get(index).type == "clue") 
        {
            //TODO handle end of game
            index++; // move to next event
            // check to see if it is a new super searcher
            setUIElements(); // set elements accordingly
                             // if it's dialogue all is done, otherwise need to get answer
            // if it was a clue take away signs
            if (events.get(index - 1).type == "clue")
            {
                contentBox.SetActive(false);
                cluePrompt.SetActive(false);
            }
            // if clue flip correct puzzle piece, show content
            if (events.get(index).type == "clue")
            {
                handleClue();
            }
            else if (events.get(index).type != "dialogue")
            {
                prepareQuestion();
            }
           
        }
        else if (events.get(index).type != "dialogue")
        {
            Debug.Log("Event is " + events.get(index).type + "and was " + events.get(index).type);
        }
    }

}
