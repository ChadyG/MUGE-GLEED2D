using System;
using System.Collections.Generic;

namespace GLEED2D
{
    enum CommandType
    {
        Transform, Add, Delete, NameChange, OrderChange, WholeLevel
    }

    class Command
    {
        public String Description;
        public CommandType ComType;
        public List<IUndoable> ObjectsBefore = new List<IUndoable>();
        public List<IUndoable> ObjectsAfter = new List<IUndoable>();

        public Command(string description)
        {
            ComType = CommandType.WholeLevel;
            Description = description;
            ObjectsBefore.Add(Editor.Instance.level.cloneforundo());
        }

        public List<IUndoable> Undo()
        {
            switch (ComType)
            {
                case CommandType.WholeLevel:
                    Editor.Instance.level = (Level)ObjectsBefore[0];
                    Editor.Instance.getSelectionFromLevel();
                    Editor.Instance.updatetreeview();
                    break;
            }
            return null;
        }

        public List<IUndoable> Redo()
        {
            switch (ComType)
            {
                case CommandType.WholeLevel:
                    Editor.Instance.level = (Level)ObjectsAfter[0];
                    Editor.Instance.getSelectionFromLevel();
                    Editor.Instance.updatetreeview();
                    break;
            }
            return null;
        }

        public void saveAfterState()
        {
            ObjectsAfter.Add(Editor.Instance.level.cloneforundo());
        }

    }
}
