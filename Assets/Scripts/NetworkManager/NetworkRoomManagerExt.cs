using UnityEngine;

namespace Mirror.Examples.NetworkRoom
{
    [AddComponentMenu("")]
    public class NetworkRoomManagerExt : NetworkRoomManager
    {
        /// <summary>
        /// Called just after GamePlayer object is instantiated and just before it replaces RoomPlayer object.
        /// This is the ideal point to pass any data like player name, credentials, tokens, colors, etc.
        /// into the GamePlayer object as it is about to enter the Online scene.
        /// </summary>
        /// <param name="roomPlayer"></param>
        /// <param name="gamePlayer"></param>
        /// <returns>true unless some code in here decides it needs to abort the replacement</returns>
        public override bool OnRoomServerSceneLoadedForPlayer(GameObject roomPlayer, GameObject gamePlayer)
        {
            NetworkServer.Destroy(roomPlayer);
            //roomPlayer.SetActive(false);
            return true;
        }

        /*
            This code below is to demonstrate how to do a Start button that only appears for the Host player
            showStartButton is a local bool that's needed because OnRoomServerPlayersReady is only fired when
            all players are ready, but if a player cancels their ready state there's no callback to set it back to false
            Therefore, allPlayersReady is used in combination with showStartButton to show/hide the Start button correctly.
            Setting showStartButton false when the button is pressed hides it in the game scene since NetworkRoomManager
            is set as DontDestroyOnLoad = true.
        */

        bool showStartButton;

        public override void OnRoomServerPlayersReady()
        {
            // calling the base method calls ServerChangeScene as soon as all players are in Ready state.
            if (isHeadless)
                base.OnRoomServerPlayersReady();
            else
                showStartButton = true;
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (allPlayersReady && showStartButton && GUI.Button(new Rect(150, 300, 120, 20), "START GAME"))
            {
                // set to false to hide it in the game scene
                showStartButton = false;

                ServerChangeScene(GameplayScene);
            }
        }




        public override void OnRoomServerSceneChanged(string sceneName)
        {

            if (sceneName.StartsWith("GameScene"))
            {
                //Find all Manager
                ItemManager[] itemManager = Resources.FindObjectsOfTypeAll<ItemManager>();

                // if there are one or more ItemManager, start the first one.
                if (itemManager.Length > 0)
                {
                    itemManager[0].OnServerStart();
                    //Destroy unused Itemmanagers
                    for (int i = 1; i < itemManager.Length; i++)
                    {
                        Destroy(itemManager[i].gameObject);
                    }
                }
            }
        }



    }
}
