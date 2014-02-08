﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorkFlow;
using WorkFlow.Entities;
using System.Linq;
using WorkFlow.Exceptions;
using System.IO;
using WorkFlowTest.Visitor;
using WorkFlow.Business.BusinessProcess;
using WorkFlow.Business.Search;
using WorkFlow.Context;
using WorkFlow.DAO;
using WorkFlow.Configuration;
using WorkFlow.Impl;

namespace WorkFlowTest
{
    [TestClass]
    public class WorkFlowTest
    {
        private static IWorkFlow work;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            WorkFlowConfiguration.Binder.SetRepository(typeof(DAOEmbeddedResource))
                .Setup(x => x.TypeName, "WorkFlow.Json.movimentacao.json , WorkFlowMachine");

            work = WorkFlowManager.GetManager();
        }

        [TestMethod]
        public void GetNextStatus()
        {
            WorkFlowContext context = work.GetContext().AddArea("BarraBotoesPDM")
                                                        .AddOperation("PedirAprovarPDM")
                                                        .AddSourceState("EMRASCUNHO");
            string status = work.GetNextStatus(context);
            Assert.AreEqual("AGUARDANDOAPROVAÇÃO", status);
        }

        [TestMethod]
        public void GetNextStatusWithAll()
        {
            WorkFlowContext context = work.GetContext().AddArea("BarraBotoesPDM")
                                                        .AddOperation("SOLICITAR_CANCELAMENTO")
                                                        .AddSourceState("EMITIDO");

            context["Finalidade"] = new List<string> { "2" };//deposito
            context["Orgao"] = new List<string> { "1" };
            string status = work.GetNextStatus(context);
            Assert.AreEqual("CANCELAMENTOSOLICITADOPDM", status);
        }

        [TestMethod]
        public void GetAtivities()
        {
            WorkFlowContext context = work.GetContext().AddArea("BarraBotoesPDM")                                                        
                                                        .AddSourceState("EMRASCUNHO");
            IList<Activity> act = work.GetActivities(context);
            Assert.AreEqual(2, act.Count);
        }

        /// <summary>
        /// Testando os status automáticos
        /// </summary>
        [TestMethod]
        public void GetDestinyWithNoOrigin()
        {
            WorkFlowContext context = work.GetContext().Reset().AddArea("Automatico")
                                                               .AddOperation("ULTIMO_ITEM_CANCELADO_PDM");

            string destiny = work.GetNextStatus(context);
            Assert.AreEqual("CANCELADOPDM", destiny);
        }

        [TestMethod]
        public void GetAtivitiesGrid()
        {
            work.GetContext().AddElements("EMRASCUNHO", "GridPDM");
            IList<Activity> act = work.GetActivities(work.GetContext());
            Assert.AreEqual(2, act.Count);
            Assert.IsTrue(act.Any(x => x.Operation.Equals("ALTERAR_ITEM_PDM")));
            Assert.IsTrue(act.Any(x => x.Operation.Equals("EXCLUIR_ITEM_PDM")));

            work.GetContext().AddSourceState("EMITIDO");
            IList<Activity> emitido = work.GetActivities(work.GetContext());
            Assert.AreEqual(0, emitido.Count);

            work.GetContext().AddSourceState("EMANALISE");
            IList<Activity> analise = work.GetActivities(work.GetContext());
            Assert.AreEqual(2, analise.Count);
            Assert.IsTrue(analise.Any(x => x.Operation.Equals("ALTERAR_ITEM_PDM")));
            Assert.IsTrue(analise.Any(x => x.Operation.Equals("CANCELAR_ITEM_PDM")));
        }

        [TestMethod]
        public void CheckConditionsKeyAndAtLeastOneValueMatch()
        {
            WorkFlowContext context = work.GetContext();

            context["Finalidade"] = new List<string> { "3" };//destruicao
            context["Orgao"] = new List<string> { "1" };
            Assert.IsTrue(context.Match.CheckConditions("DESTRUICAO", context));
        }

        [TestMethod]
        public void CheckConditionsKeyNotMatch()
        {
            WorkFlowContext context = work.GetContext();

            context["Finalidade"] = new List<string> { "2" };//deposito
            context["Orgao"] = new List<string> { "1" };
            Assert.IsFalse(context.Match.CheckConditions("DESTRUICAO", context));
        }

        [TestMethod]
        public void CheckConditionsKeyMatchButValueDoesnt()
        {
            WorkFlowContext context = work.GetContext();
            context["Finalidade"] = new List<string> { "2" };//deposito
            context["Orgao"] = new List<string> { "1" };
            Assert.IsFalse(context.Match.CheckConditions("EXTINCAO", context));
        }

        [TestMethod]
        public void SameOriginForDifferentConditions()
        {
            WorkFlowContext context = work.GetContext();
            context.AddElements("EMITIDO", "BarraBotoesPDM");
            context["Finalidade"] = new List<string> { "4" };//deposito
            context["Orgao"] = new List<string> { "1" };
            IList<Activity> lista = work.GetActivities(context);
            Assert.IsTrue(lista.Count == 2 && lista.Any(x => x.Operation.Equals("INICIO_ANALISE_EXPORTACAO")));
            /*testing another condition*/

            WorkFlowContext context2 = work.GetContext();
            context2.AddElements("EMITIDO", "BarraBotoesPDM");
            context2["Finalidade"] = new List<string> { "2" };//deposito
            context2["Orgao"] = new List<string> { "1" };
            IList<Activity> atividades = work.GetActivities(context2);
            Assert.IsTrue(atividades.Count == 2 && atividades.Any(x => x.Operation.Equals("AssumirPDM")));
        }

        [TestMethod]
        public void ConditionAndStatusAllTogetherWithStatusInBut()
        {
            WorkFlowContext context = work.GetContext().AddElements("AGUARDAMOVIMENTAÇÃO", "PADOperation");
            context["Finalidade"] = new List<string> { "2" };//deposito
            context["Orgao"] = new List<string> { "1" };
            IList<Activity> lista = work.GetActivities(context);
            Assert.AreEqual(0, lista.Count);
        }

        [TestMethod]
        public void ConditionAndStatusAllTogetherStatusNotInBut()
        {
            WorkFlowContext context = work.GetContext().AddElements("AGUARDAMOVIMENTAÇÃODESTINO", "PADOperation");
            context["Finalidade"] = new List<string> { "2" };//deposito
            context["Orgao"] = new List<string> { "1" };
            IList<Activity> lista = work.GetActivities(context);
            Assert.IsTrue(lista.Count == 1 && lista.Any(x => x.Operation.Equals("LIBERAR_MOVIMENTACAO")));
        }

        [TestMethod]
        public void ConditionMatchAndStatusAllTogetherStatusNotInBut()
        {
            WorkFlowContext context = work.GetContext().AddElements("AGUARDAMOVIMENTAÇÃODESTINO", "PADOperation");
            context["Finalidade"] = new List<string> { "3" };//destruicao
            context["Orgao"] = new List<string> { "1" };
            IList<Activity> lista = work.GetActivities(context);
            Assert.IsTrue(lista.Count == 2 && lista.Any(x => x.Operation.Equals("LIBERAR_EXTINCAO")));
        }

        [TestMethod]
        public void ImprimePADParaDeposito()
        {
            WorkFlowContext context = work.GetContext().AddElements("PADEMITIDO", "BarraBotoesPAD");
            context["Finalidade"] = new List<string> { "2" };//deposito
            context["Orgao"] = new List<string> { "2" };

            var lista = (List<string>)work.Run(context, SearchMode.Depth, new ListVisitor());
            string transicoes = string.Join(",", lista.ToArray());
            Assert.AreEqual("PADEMITIDO--[Autorizar Movimentação]-->MOVIMENTAÇÃOAUTORIZADA ,MOVIMENTAÇÃOAUTORIZADA--[Associar RT]-->None ,MOVIMENTAÇÃOAUTORIZADA--[Cancelar PAD]-->CANCELADOPAD ,MOVIMENTAÇÃOAUTORIZADA--[Movimentar Material]-->AGUARDAMOVIMENTAÇÃO ,AGUARDAMOVIMENTAÇÃO--[Associar RT]-->None ,AGUARDAMOVIMENTAÇÃO--[Cancelar PAD]-->CANCELADOPAD ,AGUARDAMOVIMENTAÇÃO--[Disponibilizar para Vistoria]-->EMVISTORIA ,EMVISTORIA--[Solicitar Apoio Técnico]-->AGUARDANDOIDENTIFICAÇÃO ,AGUARDANDOIDENTIFICAÇÃO--[Cancelar PAD]-->CANCELADOPAD ,AGUARDANDOIDENTIFICAÇÃO--[Informar apoio técnico]-->EMVISTORIA ,EMVISTORIA--[Cancelar PAD]-->CANCELADOPAD ,EMVISTORIA--[Informar Divergência]-->DIVERGENTE ,DIVERGENTE--[Cancelar PAD]-->CANCELADOPAD ,DIVERGENTE--[Confirmar Acerto]-->EMVISTORIA ,EMVISTORIA--[Concluir Vistoria]-->AGUARDAMOVIMENTAÇÃODESTINO ,AGUARDAMOVIMENTAÇÃODESTINO--[Cancelar PAD]-->CANCELADOPAD ,AGUARDAMOVIMENTAÇÃODESTINO--[Confirmar chegada]-->AGUARDARETORNO ,AGUARDARETORNO--[Cancelar PAD]-->CANCELADOPAD ,AGUARDARETORNO--[Confirmar Retorno]-->RETORNOCONFIRMADO ,RETORNOCONFIRMADO--[Confirmação de Encerramento]-->ENCERRADO ,RETORNOCONFIRMADO--[Cancelar PAD]-->CANCELADOPAD ,DIVERGENTE--[Informar Justificativa]-->MOVIMENTAÇÃOAUTORIZADA ,PADEMITIDO--[Cancelar PAD]-->CANCELADOPAD ", transicoes);
        }

        [TestMethod]
        public void ImprimePADParaDestruicao()
        {
            WorkFlowContext context = work.GetContext().AddElements("PADEMITIDO", "BarraBotoesPAD");
            context["Finalidade"] = new List<string> { "3" };//destruicao
            context["Orgao"] = new List<string> { "2" };

            var lista = (List<string>)work.Run(context, SearchMode.Breadth, new ListVisitor());
            string transicoes = string.Join(",", lista.ToArray());
            Assert.AreEqual("PADEMITIDO--[Autorizar Movimentação]-->MOVIMENTAÇÃOAUTORIZADA ,PADEMITIDO--[Cancelar PAD]-->CANCELADOPAD ,MOVIMENTAÇÃOAUTORIZADA--[Associar RT]-->None ,MOVIMENTAÇÃOAUTORIZADA--[Cancelar PAD]-->CANCELADOPAD ,MOVIMENTAÇÃOAUTORIZADA--[Movimentar Material]-->AGUARDAMOVIMENTAÇÃO ,AGUARDAMOVIMENTAÇÃO--[Associar RT]-->None ,AGUARDAMOVIMENTAÇÃO--[Cancelar PAD]-->CANCELADOPAD ,AGUARDAMOVIMENTAÇÃO--[Disponibilizar para Vistoria]-->EMVISTORIA ,EMVISTORIA--[Solicitar Apoio Técnico]-->AGUARDANDOIDENTIFICAÇÃO ,EMVISTORIA--[Cancelar PAD]-->CANCELADOPAD ,EMVISTORIA--[Informar Divergência]-->DIVERGENTE ,EMVISTORIA--[Concluir Vistoria]-->AGUARDAMOVIMENTAÇÃODESTINO ,AGUARDANDOIDENTIFICAÇÃO--[Cancelar PAD]-->CANCELADOPAD ,AGUARDANDOIDENTIFICAÇÃO--[Informar apoio técnico]-->EMVISTORIA ,DIVERGENTE--[Cancelar PAD]-->CANCELADOPAD ,DIVERGENTE--[Confirmar Acerto]-->EMVISTORIA ,DIVERGENTE--[Informar Justificativa]-->MOVIMENTAÇÃOAUTORIZADA ,AGUARDAMOVIMENTAÇÃODESTINO--[Cancelar PAD]-->CANCELADOPAD ,AGUARDAMOVIMENTAÇÃODESTINO--[Protocolar Requerimento]-->PROTOCOLOREQUERIMENTO ,PROTOCOLOREQUERIMENTO--[Aguardar Destruição]-->AGUARDANDODESTRUIÇÃO ,PROTOCOLOREQUERIMENTO--[Cancelar PAD]-->CANCELADOPAD ,AGUARDANDODESTRUIÇÃO--[Destruição Efetuada]-->ENCERRAMENTOPROCESSO ,AGUARDANDODESTRUIÇÃO--[Cancelar PAD]-->CANCELADOPAD ,ENCERRAMENTOPROCESSO--[Encerrar Destruição]-->ENCERRADO ,ENCERRAMENTOPROCESSO--[Cancelar PAD]-->CANCELADOPAD ", transicoes);
        }

      

        [TestMethod]
        public void ImprimePDMParaReexportacao()
        {
            WorkFlowContext context = work.GetContext().AddElements("EMRASCUNHO", "BarraBotoesPDM");
            context["Finalidade"] = new List<string> { "4" };//Reexportacao
            context["Orgao"] = new List<string> { "2" };

            var lista = (List<string>)work.Run(context, SearchMode.Depth, new ListVisitor());
            string transicoes = string.Join(",", lista.ToArray());
            Assert.AreEqual("EMRASCUNHO--[Apagar Rascunho]-->None ,EMRASCUNHO--[Solicitar Aprovação]-->AGUARDANDOAPROVAÇÃO ,AGUARDANDOAPROVAÇÃO--[Emitir SRM]-->EMITIDO ,EMITIDO--[Assumir SRM]-->ANALISESRMEXPORTAÇÃO ,ANALISESRMEXPORTAÇÃO--[Enviar Questionario]-->AGUARDANDOQUESTIONÁRIO ,AGUARDANDOQUESTIONÁRIO--[Questionario Preenchido]-->ANALISESRMEXPORTAÇÃO ,ANALISESRMEXPORTAÇÃO--[Retornar Análise]-->EMANALISE ,EMANALISE--[Associar PAD]-->PADGERADO ,PADGERADO--[Encerrando SRM]-->PROCESSOENCERRADO ,PADGERADO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,CANCELAMENTOSOLICITADOPDM--[Recusar Cancelamento]-->None ,EMANALISE--[Revisar SRM]-->EMREVISÃO ,EMREVISÃO--[Solicitar Aprovação]-->AGUARDANDOAPROVAÇÃO ,AGUARDANDOAPROVAÇÃO--[Não Aprovar SRM]-->EMREVISÃO ,EMREVISÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,AGUARDANDOAPROVAÇÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,EMANALISE--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,ANALISESRMEXPORTAÇÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,AGUARDANDOQUESTIONÁRIO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,EMITIDO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ", transicoes);
        }

        [TestMethod]
        public void ImprimePDMParaDeposito()
        {
            WorkFlowContext context = work.GetContext().AddElements("EMRASCUNHO", "BarraBotoesPDM");
            context["Finalidade"] = new List<string> { "2" };//Reexportacao
            context["Orgao"] = new List<string> { "2" };

            var lista = (List<string>)work.Run(context, SearchMode.Breadth, new ListVisitor());
            string transicoes = string.Join(",", lista.ToArray());
            Assert.AreEqual("EMRASCUNHO--[Apagar Rascunho]-->None ,EMRASCUNHO--[Solicitar Aprovação]-->AGUARDANDOAPROVAÇÃO ,AGUARDANDOAPROVAÇÃO--[Emitir SRM]-->EMITIDO ,AGUARDANDOAPROVAÇÃO--[Não Aprovar SRM]-->EMREVISÃO ,AGUARDANDOAPROVAÇÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,EMITIDO--[Assumir SRM]-->EMANALISE ,EMITIDO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,EMREVISÃO--[Solicitar Aprovação]-->AGUARDANDOAPROVAÇÃO ,EMREVISÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,CANCELAMENTOSOLICITADOPDM--[Recusar Cancelamento]-->None ,EMANALISE--[Associar PAD]-->PADGERADO ,EMANALISE--[Revisar SRM]-->EMREVISÃO ,EMANALISE--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,PADGERADO--[Encerrando SRM]-->PROCESSOENCERRADO ,PADGERADO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ", transicoes);
        }

        [TestMethod]
        public void ImprimePDMParaDepositoInWidth()
        {
            WorkFlowContext context = work.GetContext().AddElements("EMRASCUNHO", "BarraBotoesPDM");
            context["Finalidade"] = new List<string> { "2" };//Reexportacao
            context["Orgao"] = new List<string> { "2" };

            var lista = (List<string>)work.Run(context, SearchMode.Breadth, new ListVisitor());
            string transicoes = string.Join(",", lista.ToArray());
            Assert.AreEqual("EMRASCUNHO--[Apagar Rascunho]-->None ,EMRASCUNHO--[Solicitar Aprovação]-->AGUARDANDOAPROVAÇÃO ,AGUARDANDOAPROVAÇÃO--[Emitir SRM]-->EMITIDO ,AGUARDANDOAPROVAÇÃO--[Não Aprovar SRM]-->EMREVISÃO ,AGUARDANDOAPROVAÇÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,EMITIDO--[Assumir SRM]-->EMANALISE ,EMITIDO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,EMREVISÃO--[Solicitar Aprovação]-->AGUARDANDOAPROVAÇÃO ,EMREVISÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,CANCELAMENTOSOLICITADOPDM--[Recusar Cancelamento]-->None ,EMANALISE--[Associar PAD]-->PADGERADO ,EMANALISE--[Revisar SRM]-->EMREVISÃO ,EMANALISE--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,PADGERADO--[Encerrando SRM]-->PROCESSOENCERRADO ,PADGERADO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ", transicoes);
        }

        [TestMethod]
        public void ImprimePDMParaDepositContext()
        {
            WorkFlowContext context = work.GetContext().AddElements("EMRASCUNHO", "BarraBotoesPDM");
            context["Finalidade"] = new List<string> { "2" };//deposito
            context["Orgao"] = new List<string> { "2" };

            var lista = (List<string>)work.Run(context, SearchMode.Breadth, new ListVisitor());
            string transicoes = string.Join(",", lista.ToArray());
            Assert.AreEqual("EMRASCUNHO--[Apagar Rascunho]-->None ,EMRASCUNHO--[Solicitar Aprovação]-->AGUARDANDOAPROVAÇÃO ,AGUARDANDOAPROVAÇÃO--[Emitir SRM]-->EMITIDO ,AGUARDANDOAPROVAÇÃO--[Não Aprovar SRM]-->EMREVISÃO ,AGUARDANDOAPROVAÇÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,EMITIDO--[Assumir SRM]-->EMANALISE ,EMITIDO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,EMREVISÃO--[Solicitar Aprovação]-->AGUARDANDOAPROVAÇÃO ,EMREVISÃO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,CANCELAMENTOSOLICITADOPDM--[Recusar Cancelamento]-->None ,EMANALISE--[Associar PAD]-->PADGERADO ,EMANALISE--[Revisar SRM]-->EMREVISÃO ,EMANALISE--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ,PADGERADO--[Encerrando SRM]-->PROCESSOENCERRADO ,PADGERADO--[Solicitar Cancelamento]-->CANCELAMENTOSOLICITADOPDM ", transicoes);
                   
        }
    }
}
