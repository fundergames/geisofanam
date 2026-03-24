using UnityEngine;
using System;
using System.Collections.Generic;

namespace RogueDeal.HexLevels
{
    public class HexEditorCommandBus
    {
        private HexGrid grid;
        private HexEditorState state;
        
        private Stack<IHexEditorCommand> undoStack = new Stack<IHexEditorCommand>();
        private Stack<IHexEditorCommand> redoStack = new Stack<IHexEditorCommand>();
        
        public event Action OnCommandExecuted;
        
        public HexEditorCommandBus(HexGrid grid, HexEditorState state)
        {
            this.grid = grid;
            this.state = state;
        }
        
        public void Execute(IHexEditorCommand command)
        {
            command.Execute(grid, state);
            undoStack.Push(command);
            redoStack.Clear();
            OnCommandExecuted?.Invoke();
        }
        
        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                IHexEditorCommand command = undoStack.Pop();
                command.Undo(grid, state);
                redoStack.Push(command);
                OnCommandExecuted?.Invoke();
            }
        }
        
        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                IHexEditorCommand command = redoStack.Pop();
                command.Execute(grid, state);
                undoStack.Push(command);
                OnCommandExecuted?.Invoke();
            }
        }
        
        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
    }
    
    public interface IHexEditorCommand
    {
        void Execute(HexGrid grid, HexEditorState state);
        void Undo(HexGrid grid, HexEditorState state);
    }
    
    public class PlaceTileCommand : IHexEditorCommand
    {
        private HexCoordinate hex;
        private GameObject prefab;
        private int rotation;
        private HexTileData previousData;
        
        public PlaceTileCommand(HexCoordinate hex, GameObject prefab, int rotation)
        {
            this.hex = hex;
            this.prefab = prefab;
            this.rotation = rotation;
        }
        
        public void Execute(HexGrid grid, HexEditorState state)
        {
            previousData = grid.GetTile(hex);
            
            HexTileData newData = new HexTileData();
            Vector3 worldPos = grid.HexToWorld(hex);
            
            GameObject instance = UnityEngine.Object.Instantiate(prefab, worldPos, Quaternion.Euler(0, rotation * 60f, 0));
            newData.groundTilePrefab = prefab;
            newData.groundTileInstance = instance;
            newData.elevation = state.elevation;
            
            grid.SetTile(hex, newData);
        }
        
        public void Undo(HexGrid grid, HexEditorState state)
        {
            HexTileData currentData = grid.GetTile(hex);
            if (currentData != null && currentData.groundTileInstance != null)
            {
                UnityEngine.Object.Destroy(currentData.groundTileInstance);
            }
            
            grid.SetTile(hex, previousData);
        }
    }
    
    public class DeleteTileCommand : IHexEditorCommand
    {
        private HexCoordinate hex;
        private HexTileData previousData;
        
        public DeleteTileCommand(HexCoordinate hex)
        {
            this.hex = hex;
        }
        
        public void Execute(HexGrid grid, HexEditorState state)
        {
            previousData = grid.GetTile(hex);
            
            if (previousData != null && previousData.groundTileInstance != null)
            {
                UnityEngine.Object.Destroy(previousData.groundTileInstance);
            }
            
            grid.SetTile(hex, null);
        }
        
        public void Undo(HexGrid grid, HexEditorState state)
        {
            if (previousData != null && previousData.groundTilePrefab != null)
            {
                Vector3 worldPos = grid.HexToWorld(hex);
                GameObject instance = UnityEngine.Object.Instantiate(
                    previousData.groundTilePrefab, 
                    worldPos, 
                    Quaternion.identity
                );
                previousData.groundTileInstance = instance;
            }
            
            grid.SetTile(hex, previousData);
        }
    }
    
    public class PlaceObjectCommand : IHexEditorCommand
    {
        private HexCoordinate hex;
        private GameObject prefab;
        private int rotation;
        private GameObject instance;
        
        public PlaceObjectCommand(HexCoordinate hex, GameObject prefab, int rotation)
        {
            this.hex = hex;
            this.prefab = prefab;
            this.rotation = rotation;
        }
        
        public void Execute(HexGrid grid, HexEditorState state)
        {
            HexTileData tileData = grid.GetTile(hex);
            if (tileData == null)
            {
                tileData = new HexTileData();
            }
            
            Vector3 worldPos = grid.HexToWorld(hex);
            instance = UnityEngine.Object.Instantiate(prefab, worldPos + Vector3.up * 0.1f, Quaternion.Euler(0, rotation * 60f, 0));
            
            tileData.AddObjectLayer(prefab, instance, 0.1f);
            grid.SetTile(hex, tileData);
        }
        
        public void Undo(HexGrid grid, HexEditorState state)
        {
            if (instance != null)
            {
                UnityEngine.Object.Destroy(instance);
            }
            
            HexTileData tileData = grid.GetTile(hex);
            if (tileData != null)
            {
                tileData.RemoveObjectLayer(instance);
                if (!tileData.HasGroundTile() && !tileData.HasObjects())
                {
                    grid.SetTile(hex, null);
                }
            }
        }
    }
}
