#if NET5_0

using System.Collections;

namespace System.Configuration.Install
{
    public class InstallerCollection : CollectionBase
    {
        #region Private Fields

        private Installer _owner;

        #endregion Private Fields

        #region Protected Methods

        protected override void OnInsert(int index, object value)
        {
            if (value == _owner)
                throw new ArgumentException("CantAddSelf");
            var traceVerbose = CompModSwitches.InstallerDesign.TraceVerbose;
            ((Installer)value)._parent = _owner;
        }

        protected override void OnRemove(int index, object value)
        {
            var traceVerbose = CompModSwitches.InstallerDesign.TraceVerbose;
            ((Installer)value)._parent = null;
        }

        protected override void OnSet(int index, object oldValue, object newValue)
        {
            if (newValue == _owner)
                throw new ArgumentException("CantAddSelf");
            var traceVerbose = CompModSwitches.InstallerDesign.TraceVerbose;
            ((Installer)oldValue)._parent = null;
            ((Installer)newValue)._parent = _owner;
        }

        #endregion Protected Methods

        #region Internal Constructors

        internal InstallerCollection(Installer owner) => _owner = owner;

        #endregion Internal Constructors

        #region Public Indexers

        public Installer this[int index]
        {
            get => (Installer)List[index];
            set => List[index] = value;
        }

        #endregion Public Indexers

        #region Public Methods

        public int Add(Installer value) => List.Add(value);

        public void AddRange(InstallerCollection value)
        {
            var num = value != null ? value.Count : throw new ArgumentNullException(nameof(value));
            for (var index = 0; index < num; ++index) Add(value[index]);
        }

        public void AddRange(Installer[] value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            for (var index = 0; index < value.Length; ++index)
                Add(value[index]);
        }

        public bool Contains(Installer value) => List.Contains(value);

        public void CopyTo(Installer[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(Installer value) => List.IndexOf(value);

        public void Insert(int index, Installer value) => List.Insert(index, value);

        public void Remove(Installer value) => List.Remove(value);

        #endregion Public Methods
    }
}

#endif
