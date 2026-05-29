using System.Collections.Generic;
using NUnit.Framework;
using TimboJimboEditor;
using UnityEditor;
using UnityEngine;

namespace TimboJimboTests.PropertyBindings
{
    [TestFixture]
    public class UserEditTrackerTests
    {
        GameObject _go;
        PropertyBag _comp;
        UserEditTracker _tracker;
        List<(EditType type, BindablePropertyValueEdit edit)> _edits;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("UserEditTrackerTest");
            _comp = _go.AddComponent<PropertyBag>();
            _tracker = new UserEditTracker();
            _edits = new List<(EditType, BindablePropertyValueEdit)>();
            _tracker.StartDetecting((type, edit) => _edits.Add((type, edit)));
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.StopDetecting();
            Object.DestroyImmediate(_go);
            Undo.ClearAll();
        }

        private void RecordAndSetFloat(string fieldName, float value)
        {
            Undo.RecordObject(_comp, "Set " + fieldName);
            var field = typeof(PropertyBag).GetField(fieldName);
            field.SetValue(_comp, value);
            Undo.FlushUndoRecordObjects();
        }

        private void RecordAndSetBool(string fieldName, bool value)
        {
            Undo.RecordObject(_comp, "Set " + fieldName);
            var field = typeof(PropertyBag).GetField(fieldName);
            field.SetValue(_comp, value);
            Undo.FlushUndoRecordObjects();
        }

        private void RecordAndSetVector2(string fieldName, Vector2 value)
        {
            Undo.RecordObject(_comp, "Set " + fieldName);
            var field = typeof(PropertyBag).GetField(fieldName);
            field.SetValue(_comp, value);
            Undo.FlushUndoRecordObjects();
        }

        private void RecordAndSetVector3(string fieldName, Vector3 value)
        {
            Undo.RecordObject(_comp, "Set " + fieldName);
            var field = typeof(PropertyBag).GetField(fieldName);
            field.SetValue(_comp, value);
            Undo.FlushUndoRecordObjects();
        }

        private void RecordAndSetColor(string fieldName, Color value)
        {
            Undo.RecordObject(_comp, "Set " + fieldName);
            var field = typeof(PropertyBag).GetField(fieldName);
            field.SetValue(_comp, value);
            Undo.FlushUndoRecordObjects();
        }

        // ───────────── Detection: Scalar Types ─────────────

        [Test]
        public void Float_Edit_DetectedAsAdded()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);

            Assert.AreEqual(1, _edits.Count);
            Assert.AreEqual(EditType.Added, _edits[0].type);
            Assert.AreEqual(42f, _edits[0].edit.LatestValue.FloatValue, 0.001f);
            Assert.AreEqual(0f, _edits[0].edit.InitialValue.FloatValue, 0.001f);
        }

        [Test]
        public void Bool_Edit_DetectedAsAdded()
        {
            RecordAndSetBool(nameof(PropertyBag.Test2), true);

            Assert.AreEqual(1, _edits.Count);
            Assert.AreEqual(EditType.Added, _edits[0].type);
            Assert.IsTrue(_edits[0].edit.LatestValue.BoolValue);
            Assert.IsFalse(_edits[0].edit.InitialValue.BoolValue);
        }

        // ───────────── Detection: Vector Types ─────────────

        [Test]
        public void Vector2_Edit_DetectedAsAdded()
        {
            RecordAndSetVector2(nameof(PropertyBag.TestVector2), new Vector2(1f, 2f));

            Assert.AreEqual(1, _edits.Count);
            Assert.AreEqual(EditType.Added, _edits[0].type);
            Assert.AreEqual(new Vector2(1f, 2f), _edits[0].edit.LatestValue.Vector2Value);
        }

        [Test]
        public void Vector3_Edit_DetectedAsAdded()
        {
            RecordAndSetVector3(nameof(PropertyBag.TestVector3), new Vector3(1f, 2f, 3f));

            Assert.AreEqual(1, _edits.Count);
            Assert.AreEqual(EditType.Added, _edits[0].type);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), _edits[0].edit.LatestValue.Vector3Value);
        }

        [Test]
        public void Color_Edit_DetectedAsAdded()
        {
            RecordAndSetColor(nameof(PropertyBag.TestColor), new Color(1f, 0f, 0f, 1f));

            Assert.AreEqual(1, _edits.Count);
            Assert.AreEqual(EditType.Added, _edits[0].type);
            Assert.AreEqual(new Color(1f, 0f, 0f, 1f), _edits[0].edit.LatestValue.ColorValue);
        }

        // ───────────── Multiple Edits on Same Property ─────────────

        [Test]
        public void Float_SecondEdit_DetectedAsModified()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 10f);
            _edits.Clear();

            RecordAndSetFloat(nameof(PropertyBag.Test), 20f);

            Assert.AreEqual(1, _edits.Count);
            Assert.AreEqual(EditType.Modified, _edits[0].type);
            Assert.AreEqual(20f, _edits[0].edit.LatestValue.FloatValue, 0.001f);
            Assert.AreEqual(0f, _edits[0].edit.InitialValue.FloatValue, 0.001f);
        }

        // ───────────── Multiple Distinct Properties ─────────────

        [Test]
        public void TwoDistinctProperties_BothDetected()
        {
            RecordAndSetFloat(nameof(PropertyBag.TestFloatA), 5f);
            RecordAndSetFloat(nameof(PropertyBag.TestFloatB), 10f);

            Assert.AreEqual(2, _edits.Count);
            Assert.AreEqual(EditType.Added, _edits[0].type);
            Assert.AreEqual(EditType.Added, _edits[1].type);
        }

        // ───────────── Undo ─────────────

        [Test]
        public void Undo_SingleEdit_ReportsRemoved()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);
            Undo.IncrementCurrentGroup();
            _edits.Clear();

            Undo.PerformUndo();

            Assert.IsTrue(_edits.Count >= 1);
            Assert.AreEqual(EditType.Removed, _edits[_edits.Count - 1].type);
        }

        [Test]
        public void Undo_RestoresOriginalValue()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);
            Undo.IncrementCurrentGroup();
            _edits.Clear();

            Undo.PerformUndo();

            Assert.AreEqual(0f, _comp.Test, 0.001f);
        }

        // ───────────── Undo + Redo ─────────────

        [Test]
        public void Redo_AfterUndo_ReportsAdded()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);
            Undo.IncrementCurrentGroup();
            _edits.Clear();

            Undo.PerformUndo();
            _edits.Clear();

            Undo.PerformRedo();

            Assert.IsTrue(_edits.Count >= 1);
            Assert.AreEqual(EditType.Added, _edits[_edits.Count - 1].type);
            Assert.AreEqual(42f, _edits[_edits.Count - 1].edit.LatestValue.FloatValue, 0.001f);
        }

        [Test]
        public void Redo_AfterUndo_RestoresValue()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.AreEqual(42f, _comp.Test, 0.001f);
        }

        // ───────────── Undo Multiple Steps ─────────────

        [Test]
        public void Undo_TwoEdits_SecondUndo_ReportsRemoved()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 10f);
            Undo.IncrementCurrentGroup();
            RecordAndSetFloat(nameof(PropertyBag.Test), 20f);
            Undo.IncrementCurrentGroup();
            _edits.Clear();

            // Undo second edit (20 -> 10) - should be Modified since first edit still live
            Undo.PerformUndo();
            Assert.IsTrue(_edits.Count >= 1);
            Assert.AreEqual(EditType.Modified, _edits[_edits.Count - 1].type);

            _edits.Clear();

            // Undo first edit (10 -> 0) - should be Removed since no live edits left
            Undo.PerformUndo();
            Assert.IsTrue(_edits.Count >= 1);
            Assert.AreEqual(EditType.Removed, _edits[_edits.Count - 1].type);
        }

        // ───────────── GetChanges ─────────────

        [Test]
        public void GetChanges_ReturnsLiveEdits()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);

            var results = new List<BindablePropertyValueEdit>();
            _tracker.GetEdits(results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(42f, results[0].LatestValue.FloatValue, 0.001f);
        }

        [Test]
        public void GetChanges_AfterUndo_ReturnsEmpty()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();

            var results = new List<BindablePropertyValueEdit>();
            _tracker.GetEdits(results);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void GetChanges_AfterUndoAndRedo_ReturnsLiveEdit()
        {
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();
            Undo.PerformRedo();

            var results = new List<BindablePropertyValueEdit>();
            _tracker.GetEdits(results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(42f, results[0].LatestValue.FloatValue, 0.001f);
        }

        // ───────────── StopDetecting ─────────────

        [Test]
        public void StopDetecting_NoMoreCallbacks()
        {
            _tracker.StopDetecting();
            RecordAndSetFloat(nameof(PropertyBag.Test), 42f);

            Assert.AreEqual(0, _edits.Count);
        }

        [Test]
        public void StartDetecting_WhileAlreadyDetecting_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                _tracker.StartDetecting((_, __) => { }));
        }

        // ───────────── Bool Undo/Redo ─────────────

        [Test]
        public void Bool_Undo_ReportsRemoved()
        {
            RecordAndSetBool(nameof(PropertyBag.Test2), true);
            Undo.IncrementCurrentGroup();
            _edits.Clear();

            Undo.PerformUndo();

            Assert.IsTrue(_edits.Count >= 1);
            Assert.AreEqual(EditType.Removed, _edits[_edits.Count - 1].type);
        }

        // ───────────── Vector3 Undo/Redo ─────────────

        [Test]
        public void Vector3_Undo_ReportsRemoved()
        {
            RecordAndSetVector3(nameof(PropertyBag.TestVector3), new Vector3(1f, 2f, 3f));
            Undo.IncrementCurrentGroup();
            _edits.Clear();

            Undo.PerformUndo();

            Assert.IsTrue(_edits.Count >= 1);
            Assert.AreEqual(EditType.Removed, _edits[_edits.Count - 1].type);
        }

        [Test]
        public void Vector3_UndoRedo_ReportsAdded()
        {
            RecordAndSetVector3(nameof(PropertyBag.TestVector3), new Vector3(1f, 2f, 3f));
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();
            _edits.Clear();

            Undo.PerformRedo();

            Assert.IsTrue(_edits.Count >= 1);
            Assert.AreEqual(EditType.Added, _edits[_edits.Count - 1].type);
        }
    }
}
