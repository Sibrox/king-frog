﻿using UnityEngine;
using System;
using System.Collections.Generic;
//gpg
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
//for encoding
using System.Text;
//for extra save ui
using UnityEngine.SocialPlatforms;
//for text, remove
using UnityEngine.UI;
using KingFrog;

public class GooglePlayServices
{

    public static GooglePlayServices instance;

    public static string _saveName = "kingfrog_cloud_saving";

    public static bool loading = false;



    /*ACHIVEMENT*/
    public const string FIRST_STEP = "CgkIzqblrPINEAIQAg";
    public const string LIGHT_POCKET = "CgkIzqblrPINEAIQBw";
    public const string LEGGIANTE_POCKET = "CgkIzqblrPINEAIQCA";
    public const string HEAVY_POCKET = "CgkIzqblrPINEAIQCQ";
    public const string MADALINA = "CgkIzqblrPINEAIQCg";
    public const string DARK_KNIGHT = "CgkIzqblrPINEAIQCw";
    public const string ARTIST = "CgkIzqblrPINEAIQDA";
    public const string XMAS = "CgkIzqblrPINEAIQDQ";


    public static bool Authenticated
    {
        get
        {
            return Social.Active.localUser.authenticated;
        }
    }

    //METODO RICHIAMATO PER IL LOGIN NEL PLAY SERVICES
    public static void Authentication()
    {
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
        .EnableSavedGames()
        .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate();
        Social.localUser.Authenticate((bool success) => {
            // handle success or failure
            if (success)
            {
                Debug.Log("logged");
            }
            else
            {
                Debug.Log("Not Logged");
            }
        });
    }

    //QUESTO METODO APRE IL METADATA PER IL SALVATAGGIO, SUBITO DOPO RICHIAMA SaveGameOpened()
    public static void SaveToCloud()
    {

        if (Authenticated)
        {
            Debug.Log("Saving progress to the cloud... filename: ");
            //save to named file
            ((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(
                _saveName, //name of file. If save doesn't exist it will be created with this name
                DataSource.ReadNetworkOnly,
                ConflictResolutionStrategy.UseMostRecentlySaved,
                SavedGameOpened);
        }
        else
        {
            Debug.Log("Not authenticated!");
        }

    }

    //METODO PER IL LOAD DAL CLOUD DEL SALVATAGGIO
    public static void LoadFromCloud()
    {
        if (Authenticated)
        {
            loading = true;
            Debug.Log("Loading game progress from the cloud.");
            ((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(
                _saveName, //name of file.
                DataSource.ReadNetworkOnly,
                ConflictResolutionStrategy.UseMostRecentlySaved,
                LoadGameAfterOpen);
        }
        else
        {
            if (GameManager.instance.gameSaveData == null)
            {
                GameManager.instance.gameSaveData = new GameSaved();
                GameManager.instance.firstRun = true;
                GameManager.instance.SetSave();
            }
        }
    }

    //METODO RICHIAMATO DOPO L'APERTURA DEL METADATA DA SOVRASCRIVERE
    private static void LoadGameAfterOpen(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        //check success
        if (status == SavedGameRequestStatus.Success)
        {
            //create builder. here you can add play time, time created etc for UI.
            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
            SavedGameMetadataUpdate updatedMetadata = builder.Build();

            //Load from cloud
            ((PlayGamesPlatform)Social.Active).SavedGame.ReadBinaryData(game, SavedGameLoaded);
            //loading
        }   
        else
        {
            if (GameManager.instance.gameSaveData == null)
            {
                GameManager.instance.gameSaveData = new GameSaved();
                GameManager.instance.firstRun = true;
                GameManager.instance.SetSave();
            }
        }
    }

    private static void SavedGameLoaded(SavedGameRequestStatus status, byte[] data)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            Debug.Log("SaveGameLoaded, success=" + status);

            // data è un vettore di byte che va trasformato in una stringa json
            string json = FromBytes(data);
            if (json.CompareTo("") != 0)
            {
                GameSaved tmp = JsonUtility.FromJson<GameSaved>(json);


                GameManager.instance.gameSaveData = MergeSave(tmp);
                loading = false;
                GameManager.instance.SetSave();
            }
            else if(GameManager.instance.gameSaveData == null)
            {
                Debug.LogWarning("Error reading game: " + status);
                GameManager.instance.gameSaveData = new GameSaved();
                GameManager.instance.firstRun = true;
                GameManager.instance.SetSave();

                loading = false;
            }
            else
            {
                loading = false;
                GameManager.instance.SetSave();
            }
        }
        else
        {
            if (GameManager.instance.gameSaveData == null)
            {
                Debug.LogWarning("Error reading game: " + status);
                GameManager.instance.gameSaveData = new GameSaved();
                GameManager.instance.firstRun = true;
                GameManager.instance.SetSave();
            }
            loading = false;
        }
    }

    //QUESTA FUNZIONE VIENE CHIAMATA QUANDO IL GIOCO VIENE APERTO E SI VUOLE SALVARE NEL CLOUD
    private static void SavedGameOpened(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        //check success
        if (status == SavedGameRequestStatus.Success)
        {
            //read bytes from save
            string textSave = JsonUtility.ToJson(GameManager.instance.gameSaveData);
            byte[] data = ToBytes(textSave);
            //create builder. here you can add play time, time created etc for UI.
            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
            SavedGameMetadataUpdate updatedMetadata = builder.Build();
            //saving to cloud
            ((PlayGamesPlatform)Social.Active).SavedGame.CommitUpdate(game, updatedMetadata, data, SavedGameWritten);
        }
    }

    //QUESTA FUNZIONE VIENE CHIAMATA DOPO IL COMMIT SUL CLOUD DEL SALVATAGGIO
    private static void SavedGameWritten(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            Debug.Log("Game " + game.Description + " written");
        }
        else
        {
            Debug.LogWarning("Error saving game: " + status);
        }
    }

    //return saveString as bytes
    private static byte[] ToBytes(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return bytes;
    }

    //take bytes as arg and return string
    private static string FromBytes(byte[] bytes)
    {
        string decodedString = Encoding.UTF8.GetString(bytes);
        return decodedString;
    }


    public static void UnlockFirstStep()
    {
        Social.ReportProgress("CgkIzqblrPINEAIQAg", 100.0f, (bool success) => {
            // handle success or failure
        });
    }

    public static GameSaved MergeSave(GameSaved cloudSave)
    {
        GameSaved final = cloudSave;
        int countCloud = 0, countGame = 0;

        if(GameManager.instance.gameSaveData == null)
        {
            return cloudSave;
        }
        if(GameManager.instance.gameSaveData.lastSolved > cloudSave.lastSolved)
        {
            final.lastSolved = GameManager.instance.gameSaveData.lastSolved;
        }
        
        for(int i = 0; i < 4; i++)
        {
            if (cloudSave.events[0].solved[i]) countCloud++;
            if (GameManager.instance.gameSaveData.events[0].solved[i]) countGame++;
        }

        if(countGame > countCloud)
        {
            final.events[0].solved = GameManager.instance.gameSaveData.events[0].solved;
        }

        return final;
    }


    public static void UnlockAchivement(string achivement)
    {
        if (!Authenticated)
        {
            Debug.Log("Not connected on Play Services.");
            return;
        }

        Social.ReportProgress(achivement, 100.0f, (bool success) =>
        {
            // handle success or failure
            if (success)
            {
                Debug.Log("Achivement unlocked!");
            }
            else
            {
                Debug.Log("Not succeded");
            }
        });
    }
}
