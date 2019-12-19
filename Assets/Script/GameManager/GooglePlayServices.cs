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

public class GooglePlayServices
{

    public static GooglePlayServices instance;

    public string _saveName = "kingfrog_cloud_saving";

    private bool Authenticated
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
    public void SaveToCloud()
    {

        if (Authenticated)
        {
            Debug.Log("Saving progress to the cloud... filename: ");
            //save to named file
            ((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(
                _saveName, //name of file. If save doesn't exist it will be created with this name
                DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime,
                SavedGameOpened);
        }
        else
        {
            Debug.Log("Not authenticated!");
        }

    }

    //METODO PER IL LOAD DAL CLOUD DEL SALVATAGGIO
    public void LoadFromCloud()
    {
        Debug.Log("Loading game progress from the cloud.");
        ((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(
            _saveName, //name of file.
            DataSource.ReadCacheOrNetwork,
            ConflictResolutionStrategy.UseLongestPlaytime,
            LoadGameAfterOpen);
    }

    //METODO RICHIAMATO DOPO L'APERTURA DEL METADATA DA SOVRASCRIVERE
    private void LoadGameAfterOpen(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        //check success
        if (status == SavedGameRequestStatus.Success)
        {
            //read bytes from save
            byte[] data = ToBytes(GameManager.instance.gameSaveData.ToString());
            //create builder. here you can add play time, time created etc for UI.
            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
            SavedGameMetadataUpdate updatedMetadata = builder.Build();
            //saving to cloud
            ((PlayGamesPlatform)Social.Active).SavedGame.ReadBinaryData(game, SavedGameLoaded);
            //loading
        }
    }

    private void SavedGameLoaded(SavedGameRequestStatus status, byte[] data)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            Debug.Log("SaveGameLoaded, success=" + status);

            // data è un vettore di byte che va trasformato in una stringa json
            string json = FromBytes(data);
            GameManager.instance.gameSaveData = JsonUtility.FromJson<GameSaved>(json);
        }
        else
        {
            Debug.LogWarning("Error reading game: " + status);
        }
    }

    //QUESTA FUNZIONE VIENE CHIAMATA QUANDO IL GIOCO VIENE APERTO E SI VUOLE SALVARE NEL CLOUD
    private void SavedGameOpened(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        //check success
        if (status == SavedGameRequestStatus.Success)
        {
            //read bytes from save
            byte[] data = ToBytes(GameManager.instance.gameSaveData.ToString());
            //create builder. here you can add play time, time created etc for UI.
            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
            SavedGameMetadataUpdate updatedMetadata = builder.Build();
            //saving to cloud
            ((PlayGamesPlatform)Social.Active).SavedGame.CommitUpdate(game, updatedMetadata, data, SavedGameWritten);
            //loading
        }
    }

    //QUESTA FUNZIONE VIENE CHIAMATA DOPO IL COMMIT SUL CLOUD DEL SALVATAGGIO
    private void SavedGameWritten(SavedGameRequestStatus status, ISavedGameMetadata game)
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
    private byte[] ToBytes(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return bytes;
    }

    //take bytes as arg and return string
    private string FromBytes(byte[] bytes)
    {
        string decodedString = Encoding.UTF8.GetString(bytes);
        return decodedString;
    }


    public void UnlockFirstStep()
    {
        Social.ReportProgress("CgkIzqblrPINEAIQAg", 100.0f, (bool success) => {
            // handle success or failure
        });
    }
}
