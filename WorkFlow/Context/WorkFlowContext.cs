﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorkFlow.ConcreteCondition;
using WorkFlow.Entities;

namespace WorkFlow.Context
{
    public class WorkFlowContext
    {
        Dictionary<string, List<string>> parameter = new Dictionary<string, List<string>>();

        private Structure node;
        public IMatchCondition Match { get; set; }

        public string Area { get; set; }
        public string Operation { get; set; }
        public string SourceState { get; set; }

        public WorkFlowContext()
        {
            node = WorkFlow.Singleton.WorkFlowSingleton.Instance().GetStructure();
        }

        /// <summary>
        /// Reset WorkFlow Context
        /// </summary>
        /// <returns></returns>
        public WorkFlowContext Reset()
        {
            this.Area = String.Empty;
            this.Operation = String.Empty;
            this.SourceState = String.Empty;
            parameter.Clear();
            return this;
        }

        public WorkFlowContext AddArea(string area)
        {
            this.Area = area;
            return this;
        }

        public WorkFlowContext AddSourceState(string source)
        {
            this.SourceState = source;
            return this;
        }

        public WorkFlowContext AddOperation(string operation)
        {
            this.Operation = operation;
            return this;
        }

        public Structure GetNode()
        {
            Match = new MatchCondition();
            return node;
        }

        public List<string> this[string index]
        {
            get
            {
                return parameter[index];
            }

            set
            {
                parameter[index] = value;
            }
        }

        public int Count
        {
            get { return parameter.Count; }
        }

        public Dictionary<string, List<string>>.KeyCollection Keys
        {
            get {
                return parameter.Keys;
            }
        }

        public WorkFlowContext SetCondition(Type type)
        {
            this.Match = (IMatchCondition)Activator.CreateInstance(type);
            return this;
        }

        public WorkFlowContext AddElements(string sourceState, string area)
        {
            this.SourceState = sourceState;
            this.Area = area;
            return this;
        }
    }
}
