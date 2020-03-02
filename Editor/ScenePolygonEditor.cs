/* Polygon2DDrawer.cs
 * © Eddie Cameron 2019
 * ----------------------------
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Random = UnityEngine.Random;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using System.Linq;

namespace Polygon2D {
    [CustomEditor( typeof( ScenePolygon ) )]
    [CanEditMultipleObjects]
    public class ScenePolygonEditor : Editor {

        const string POLYGON_PROP_NAME = "_polygon";
        const string VERTICES_PROP_NAME = "vertices";

        bool isFoldout;
        bool isEditing;
        Polygon2DEditUtility polygonEditor = new Polygon2DEditUtility();

        private static GUIStyle ToggleButtonStyleNormal = null;
        private static GUIStyle ToggleButtonStyleToggled = null;

        public override void OnInspectorGUI() {
            if ( ToggleButtonStyleToggled == null ) {
                ToggleButtonStyleNormal = "Button";
                ToggleButtonStyleToggled = new GUIStyle( ToggleButtonStyleNormal );
                ToggleButtonStyleToggled.normal.background = ToggleButtonStyleToggled.active.background;
            }

            serializedObject.Update();

            SerializedProperty polygonProp = serializedObject.FindProperty( POLYGON_PROP_NAME );
            SerializedProperty verticesProp = polygonProp.FindPropertyRelative( VERTICES_PROP_NAME );

            EditorGUILayout.PropertyField( verticesProp, includeChildren: true );

            // edit mode
            using ( new EditorGUI.DisabledScope( targets.Length > 1 ) ) {
                EditorGUILayout.EditorToolbarForTarget( EditorGUIUtility.TrTempContent( "Edit Polygon" ), target );
            }

            serializedObject.ApplyModifiedProperties();
        }


        [EditorTool( "Edit Polygon Zone", typeof( ScenePolygon ) )]
        public class ScenePolygonEditorTool : EditorTool {
            internal readonly Polygon2DEditUtility polyUtility = new Polygon2DEditUtility();

            public override GUIContent toolbarIcon => new GUIContent( EditorGUIUtility.IconContent( "EditCollider" ).image, "Edit Polygon" );

            public void OnEnable() {
                EditorTools.activeToolChanged += OnActiveToolChanged;
                EditorTools.activeToolChanging += OnActiveToolChanging;
                Selection.selectionChanged += OnSelectionChanged;
            }

            public void OnDisable() {
                EditorTools.activeToolChanged -= OnActiveToolChanged;
                EditorTools.activeToolChanging -= OnActiveToolChanging;
                Selection.selectionChanged -= OnSelectionChanged;
            }

            public override void OnToolGUI( EditorWindow window ) {
                Undo.RecordObject( target, "Edit Polygon" );
                if ( polyUtility.OnSceneGUI() )
                    EditorUtility.SetDirty( target );
            }

            void OnActiveToolChanged() {
                var scenePolygon = target as ScenePolygon;
                if ( EditorTools.IsActiveTool( this ) && IsAvailable() && scenePolygon )
                    polyUtility.StartEditing( scenePolygon );
            }

            void OnSelectionChanged() {
                if ( EditorTools.IsActiveTool( this ) )
                    polyUtility.StopEditing();
                var scenePolygon = target as ScenePolygon;
                if ( EditorTools.IsActiveTool( this ) && IsAvailable() && scenePolygon != null )
                    polyUtility.StartEditing( scenePolygon );
            }

            void OnActiveToolChanging() {
                if ( EditorTools.IsActiveTool( this ) )
                    polyUtility.StopEditing();
            }

            public override bool IsAvailable() {
                // We don't support multi-selection editing
                return targets.Count() == 1;
            }
        }
    }
}
