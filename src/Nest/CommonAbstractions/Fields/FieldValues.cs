﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nest
{
	[JsonConverter(typeof(FieldValuesJsonConverter))]
	public class FieldValues : IsADictionaryBase<string, object>
	{
		private readonly Inferrer _inferrer;

		public FieldValues(Inferrer inferrer, IDictionary<string, object> container)
			: base(container)
		{
			_inferrer = inferrer;
		}

		public K Value<K>(Field field)
		{
			var values = ValuesOf<K>(field);
			return values != null
				? values.FirstOrDefault()
				: default(K);
		}

		public K ValueOf<T, K>(Expression<Func<T, K>> objectPath)
			where T : class
		{
			var values = Values(objectPath);
			return values != null
				? values.FirstOrDefault()
				: default(K);
		}

		public K[] ValuesOf<K>(Field field)
		{
			var path = this._inferrer.Field(field);
			return this.FieldArray<K[]>(path);
		}

		public K[] Values<T, K>(Expression<Func<T, K>> objectPath)
			where T : class
		{
			var field = this._inferrer.Field(objectPath);
			return this.FieldArray<K[]>(field);
		}


		private static JsonSerializer ForceNoDateInferrence = new JsonSerializer
		{
			DateParseHandling = DateParseHandling.None
		};

		private K FieldArray<K>(string field)
		{
			object o;
			if (this.BackingDictionary != null && this.BackingDictionary.TryGetValue(field, out o))
			{
				var t = typeof(K);
				if (o is JArray && t.GetInterfaces().Contains(typeof(IEnumerable)))
				{
					var array = (JArray)o;
					if (typeof(K) == typeof(string[]) && array.Count > 0 && array.Any(p=>p.Type == JTokenType.Date))
					{
						// https://github.com/elastic/elasticsearch-net/issues/2155
						// o of type JArray has already decided the values are dates so there is no
						// way around this.
						// incredibly ugly and sad but the only way I found to cover this edgecase
						var s = array.Root.ToString();
						using (var sr = new StringReader(s))
						using (var jr = new JsonTextReader(sr) { DateParseHandling = DateParseHandling.None })
							return ForceNoDateInferrence.Deserialize<K>(jr);
					}
					return array.ToObject<K>();
				}
				return (K)Convert.ChangeType(o, typeof(K));
			}
			return default(K);
		}
	}
}
