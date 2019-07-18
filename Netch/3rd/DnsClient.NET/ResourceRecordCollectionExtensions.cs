using DnsClient.Protocol;
using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/> where <c>T</c> is <see cref="DnsResourceRecord"/>.
    /// </summary>
    public static class RecordCollectionExtension
    {
        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="ARecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="ARecord"/>.</returns>
        public static IEnumerable<ARecord> ARecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<ARecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="NsRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="NsRecord"/>.</returns>
        public static IEnumerable<NsRecord> NsRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<NsRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="CNameRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="CNameRecord"/>.</returns>
        public static IEnumerable<CNameRecord> CnameRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<CNameRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="SoaRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="SoaRecord"/>.</returns>

        public static IEnumerable<SoaRecord> SoaRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<SoaRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="MbRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="MbRecord"/>.</returns>
        public static IEnumerable<MbRecord> MbRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<MbRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="MgRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="MgRecord"/>.</returns>
        public static IEnumerable<MgRecord> MgRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<MgRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="MrRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="MrRecord"/>.</returns>
        public static IEnumerable<MrRecord> MrRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<MrRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="NullRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="NullRecord"/>.</returns>
        public static IEnumerable<NullRecord> NullRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<NullRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="WksRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="WksRecord"/>.</returns>
        public static IEnumerable<WksRecord> WksRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<WksRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="PtrRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="PtrRecord"/>.</returns>
        public static IEnumerable<PtrRecord> PtrRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<PtrRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="HInfoRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="HInfoRecord"/>.</returns>
        public static IEnumerable<HInfoRecord> HInfoRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<HInfoRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="MxRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="MxRecord"/>.</returns>

        public static IEnumerable<MxRecord> MxRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<MxRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="TxtRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="TxtRecord"/>.</returns>
        public static IEnumerable<TxtRecord> TxtRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<TxtRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="RpRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="RpRecord"/>.</returns>
        public static IEnumerable<RpRecord> RpRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<RpRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="AfsDbRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="AfsDbRecord"/>.</returns>
        public static IEnumerable<AfsDbRecord> AfsDbRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<AfsDbRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="AaaaRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="AaaaRecord"/>.</returns>
        public static IEnumerable<AaaaRecord> AaaaRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<AaaaRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="SrvRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="SrvRecord"/>.</returns>
        public static IEnumerable<SrvRecord> SrvRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<SrvRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="UriRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="UriRecord"/>.</returns>
        public static IEnumerable<UriRecord> UriRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<UriRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="CaaRecord"/>s only.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>The list of <see cref="CaaRecord"/>.</returns>
        public static IEnumerable<CaaRecord> CaaRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<CaaRecord>();
        }

        /// <summary>
        /// Filters the elements of an <see cref="IEnumerable{T}"/> to return <see cref="DnsResourceRecord"/>s
        /// which have the <paramref name="type"/>.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <param name="type">The <see cref="ResourceRecordType"/> to filter for.</param>
        /// <returns>The list of <see cref="ARecord"/>.</returns>
        public static IEnumerable<DnsResourceRecord> OfRecordType(this IEnumerable<DnsResourceRecord> records, ResourceRecordType type)
        {
            return records.Where(p => p.RecordType == type);
        }
    }
}