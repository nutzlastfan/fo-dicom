﻿// Copyright (c) 2012-2020 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System.Collections.Generic;
using System.Linq;

using Dicom.IO;
using Dicom.IO.Reader;
using Dicom.IO.Buffer;

namespace Dicom.Media
{
    public class DicomDirectoryReaderObserver : IDicomReaderObserver
    {
        private DicomSequence _directoryRecordSequence = null;

        private readonly Stack<DicomTag> _currentSequenceTag = new Stack<DicomTag>();

        private readonly Dictionary<uint, DicomDataset> _lookup = new Dictionary<uint, DicomDataset>();

        private readonly DicomDataset _dataset;

        public DicomDirectoryReaderObserver(DicomDataset dataset)
        {
            _dataset = dataset;
        }

        public DicomDirectoryRecord BuildDirectoryRecords()
        {
            var offset = _dataset.GetSingleValue<uint>(DicomTag.OffsetOfTheFirstDirectoryRecordOfTheRootDirectoryEntity);
            return ParseDirectoryRecord(offset);
        }

        private DicomDirectoryRecord ParseDirectoryRecord(uint offset)
        {
            DicomDirectoryRecord record = null;
            if (_lookup.ContainsKey(offset))
            {
                record = new DicomDirectoryRecord(_lookup[offset])
                {
                    Offset = offset
                };

                record.NextDirectoryRecord =
                    ParseDirectoryRecord(record.GetSingleValue<uint>(DicomTag.OffsetOfTheNextDirectoryRecord));

                record.LowerLevelDirectoryRecord =
                    ParseDirectoryRecord(record.GetSingleValue<uint>(DicomTag.OffsetOfReferencedLowerLevelDirectoryEntity));
            }

            return record;
        }

        #region IDicomReaderObserver Implementation

        public void OnElement(IByteSource source, DicomTag tag, DicomVR vr, IByteBuffer data)
        {
            // do nothing here
        }

        public void OnBeginSequence(IByteSource source, DicomTag tag, uint length)
        {
            _currentSequenceTag.Push(tag);
            if (tag == DicomTag.DirectoryRecordSequence)
            {
                _directoryRecordSequence = _dataset.GetDicomItem<DicomSequence>(tag);
            }
        }

        public void OnBeginSequenceItem(IByteSource source, uint length)
        {
            if (_currentSequenceTag.Peek() == DicomTag.DirectoryRecordSequence && _directoryRecordSequence != null)
            {
                _lookup.Add((uint)source.Position - 8, _directoryRecordSequence.LastOrDefault());
            }
        }

        public void OnEndSequenceItem()
        {
            // do nothing here
        }

        public void OnEndSequence()
        {
            _currentSequenceTag.Pop();
        }

        public void OnBeginFragmentSequence(IByteSource source, DicomTag tag, DicomVR vr)
        {
            // do nothing here
        }

        public void OnFragmentSequenceItem(IByteSource source, IByteBuffer data)
        {
            // do nothing here
        }

        public void OnEndFragmentSequence()
        {
            // do nothing here
        }

        #endregion
    }
}
