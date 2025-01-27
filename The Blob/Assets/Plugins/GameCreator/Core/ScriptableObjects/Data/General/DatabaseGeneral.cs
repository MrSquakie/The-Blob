﻿namespace GameCreator.Core
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
    using UnityEngine.Serialization;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class DatabaseGeneral : IDatabase
    {
        public enum GENERAL_SCREEN_SPACE
        {
            ScreenSpaceOverlay,
            ScreenSpaceCamera,
            WorldSpaceCamera
        }

        private const string PROVIDER_PATH = "GameCreator/provider.gamecreator.default";

        // PROPERTIES: ----------------------------------------------------------------------------

        public GENERAL_SCREEN_SPACE generalRenderMode = GENERAL_SCREEN_SPACE.ScreenSpaceOverlay;

        [FormerlySerializedAs("prefabMessage")]
        public GameObject prefabSimpleMessage;
        public GameObject prefabFloatingMessage;
        public GameObject prefabTouchstick;

        [Tooltip("Should saving/loading a game store/restore which scene the player was in?")]
        public bool saveScenes = true;
        [SerializeField] private IDataProvider provider;

        public float toolbarPositionX = 10f;
        public float toolbarPositionY = 10f;

        // PUBLIC STATIC METHODS: -----------------------------------------------------------------

        public static DatabaseGeneral Load()
        {
            return IDatabase.LoadDatabase<DatabaseGeneral>();
        }

        public IDataProvider GetDataProvider()
        {
            if (this.provider == null)
            {
                this.provider = Resources.Load<IDataProvider>(
                    PROVIDER_PATH
                );
            }

            return this.provider;
        }

        public void ChangeDataProvider(IDataProvider provider)
        {
            if (provider == null) return;
            this.provider = provider;
        }

        // OVERRIDE METHODS: ----------------------------------------------------------------------

        #if UNITY_EDITOR

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            IDatabase.Setup<DatabaseGeneral>();
        }

        public override int GetSidebarPriority()
        {
            return 1;
        }

        #endif
    }
}