﻿using System;
using System.Collections.Generic;
using System.Data;
using Plato.Internal.Abstractions;
using Plato.Internal.Abstractions.Extensions;

namespace Plato.Entities.Models
{
    public class Entity : EntityBase
    {
  
        public int FeatureId { get; set; }
        
        public string Title { get; set; }

        public string TitleNormalized { get; set; }
        
        public IList<EntityData> Data { get; set; } = new List<EntityData>();
        
        public IDictionary<Type, ISerializable> MetaData => _metaData;


        private readonly IDictionary<Type, ISerializable> _metaData;
        
        public Entity()
        {
            // TODO: Replace with concurrent dictionary
            _metaData = new Dictionary<Type, ISerializable>();
        }
        
        public void SetMetaData<T>(T obj) where T : class
        {
            _metaData.Add(typeof(T), (ISerializable)obj);
        }

        public void SetMetaData(Type type, ISerializable obj)
        {
            _metaData.Add(type, obj);
        }

        public T GetMetaData<T>() where T : class
        {
            if (_metaData.ContainsKey(typeof(T)))
            {
                return (T)_metaData[typeof(T)];
            }

            return default(T);

        }
        
        public override void PopulateModel(IDataReader dr)
        {

            base.PopulateModel(dr);

            if (dr.ColumnIsNotNull("FeatureId"))
                FeatureId = Convert.ToInt32(dr["FeatureId"]);
            
            if (dr.ColumnIsNotNull("Title"))
                Title = Convert.ToString(dr["Title"]);

            if (dr.ColumnIsNotNull("TitleNormalized"))
                TitleNormalized = Convert.ToString(dr["TitleNormalized"]);

        }

    }
    
}