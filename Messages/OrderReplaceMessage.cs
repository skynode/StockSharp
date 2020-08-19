#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderReplaceMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// The message containing the information for modify order.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderReplaceMessage : OrderRegisterMessage
	{
		/// <summary>
		/// Modified order id.
		/// </summary>
		[DataMember]
		public long? OldOrderId { get; set; }

		/// <summary>
		/// Modified order id (as a string if the electronic board does not use a numeric representation of the identifiers).
		/// </summary>
		[DataMember]
		public string OldOrderStringId { get; set; }

		/// <summary>
		/// Replaced price.
		/// </summary>
		[DataMember]
		public decimal? OldOrderPrice { get; set; }

		/// <summary>
		/// Replaced volume.
		/// </summary>
		[DataMember]
		public decimal? OldOrderVolume { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderReplaceMessage"/>.
		/// </summary>
		public OrderReplaceMessage()
			: base(MessageTypes.OrderReplace)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderReplaceMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderReplaceMessage();

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(OrderReplaceMessage destination)
		{
			base.CopyTo(destination);

			destination.OldOrderId = OldOrderId;
			destination.OldOrderStringId = OldOrderStringId;
			destination.OldOrderPrice = OldOrderPrice;
			destination.OldOrderVolume = OldOrderVolume;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString();

			if (OldOrderId != null || !OldOrderStringId.IsEmpty())
				str += $"OldOrdId={OldOrderId}/{OldOrderStringId}";

			if (OldOrderPrice != null)
				str += $"OldOrderPrice={OldOrderPrice.Value}";

			if (OldOrderVolume != null)
				str += $"OldOrderVol={OldOrderVolume.Value}";

			return str;
		}
	}
}