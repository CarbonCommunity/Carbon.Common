using ConVar;
using ProtoBuf;
using Pool = Facepunch.Pool;

namespace Carbon.Common.Carbon.Components;

public class EntitySerializer
{


	//Entity serialization
	public static ProtoBuf.BaseEntity BaseEntityToProto(BaseEntity entity)
	{
		if (entity == null) return null;

		try
		{
			var info = new BaseNetworkable.SaveInfo
			{
				forDisk = true,
				msg = Facepunch.Pool.Get<ProtoBuf.Entity>()
			};

			entity.Save(info);
			return info.msg.baseEntity;
		}
		catch (Exception e)
		{
			Logger.Error($"Failed to convert BaseEntity to ProtoBuf.BaseEntity: {e}");
			return null;
		}
	}

	public static Byte[] SerializeEntity(BaseEntity entity)
	{
		var proto = BaseEntityToProto(entity);
		if (proto == null) return null;

		try
		{
			return proto.ToProtoBytes();
		}
		catch (Exception e)
		{
			Logger.Error(e);
			return null;
		}
	}


	// Deserialization
	public static ProtoBuf.Entity ConvertToDataEntity(Byte[] data)
	{
		if (data == null) {return null;}

		BufferStream bufferStream = Pool.Get<BufferStream>().Initialize();
		bufferStream._isBufferOwned = true;
		bufferStream._buffer = data;
		bufferStream._length = bufferStream._buffer.Length;
		bufferStream._position = 0;
		ProtoBuf.Entity entity = ProtoBuf.Entity.Deserialize(bufferStream);
		bufferStream.Dispose();
		return entity;
	}

}
