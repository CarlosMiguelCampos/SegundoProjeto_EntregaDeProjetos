﻿using CamundaClient;
using CamundaClient.Dto;
using CMA.ISMAI.Core.Interface;
using CMA.ISMAI.Delivery.FileLoading.CrossCutting.Camunda.Interface;
using CMA.ISMAI.Delivery.FileLoading.Domain.Model;
using MediatR;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CMA.ISMAI.Delivery.FileLoading.CrossCutting.Camunda.Service
{
    public class CamundaService : ICamundaService
    {
        private readonly CamundaEngineClient camundaEngineClient;
        private readonly string filePath;
        private Timer pollingTimer;
        private readonly IDictionary<string, Action<ExternalTask>> workers;
        private readonly IMediator _mediator;
        private readonly INotificationService _notificationService;

        public CamundaService(IMediator mediator, INotificationService notificationService)
        {
            camundaEngineClient = new CamundaEngineClient(new System.Uri("http://localhost:8080/engine-rest/engine/default/"), null, null);
            filePath = $"CMA.ISMAI.Delivery.FileLoading.CrossCutting.Camunda.WorkFlow.FileLoadingISMAI.bpmn";
            workers = new Dictionary<string, Action<ExternalTask>>();
            _mediator = mediator;
            _notificationService = notificationService;
        }

        public bool StartWorkFlow(Core.Model.Delivery delivery)
        {
            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters = AddValuesToTheDictionary(parameters, delivery);
                FileParameter file = FileParameter.FromManifestResource(Assembly.GetExecutingAssembly(), filePath);
                string deployId = camundaEngineClient.RepositoryService.Deploy("FileLoading", new List<object> {
                    file });
                if (TheDeployWasDone(deployId))
                {
                    camundaEngineClient.BpmnWorkflowService.StartProcessInstance("FileLoading", parameters);
                }
                return true;
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        private Dictionary<string, object> AddValuesToTheDictionary(Dictionary<string, object> parameters, Core.Model.Delivery message)
        {
            foreach (PropertyInfo propertyInfo in message.GetType().GetProperties())
                parameters.Add(propertyInfo.Name, propertyInfo.GetValue(message, null));
            parameters.Add("download", message.GetType() == typeof(Core.Model.DeliveryWithLink));

            return parameters;
        }

        private bool TheDeployWasDone(string deployId)
        {
            return deployId != string.Empty;
        }

        public Variable returnVariableValue(Dictionary<string, Variable> externalTaskVariables, string key)
        {
            var delivery = externalTaskVariables;
            var getValue = new Variable();
            delivery.TryGetValue(key, out getValue);
            return getValue;
        }

        public void RegistWorkers()
        {

            registerWorker("download_files", async externalTask =>
            {
                var delivery = externalTask.Variables;
                var getStudentName = returnVariableValue(delivery, "studentName");
                var getDeliveryUrl = returnVariableValue(delivery, "fileUrl");
                var getInstituteName = returnVariableValue(delivery, "instituteName");
                var getStudentEmail = returnVariableValue(delivery, "studentEmail");
                var getCourseName = returnVariableValue(delivery, "courseName");
                var getStudentNumber = returnVariableValue(delivery, "studentNumber");
                var getDeliveryTime = returnVariableValue(delivery, "deliveryTime");
                var getCordenatorName = returnVariableValue(delivery, "cordenatorName");
                var getTitle = returnVariableValue(delivery, "title");
                var getDefenition = returnVariableValue(delivery, "defenitionOfDelivery");
                var getpublicdefenition = returnVariableValue(delivery, "publicPDFVersionName");
                var getprivatedefenition = returnVariableValue(delivery, "privatePDFVersionName");
                var getId = returnVariableValue(delivery, "id");
                DownloadFileFromUrlCommand downloadCommand = new DownloadFileFromUrlCommand(Guid.Parse(getId.Value.ToString())
                    , getStudentName.Value.ToString(), getInstituteName.Value.ToString(), getCourseName.Value.ToString(), getStudentEmail.Value.ToString(), getStudentNumber.Value.ToString(), DateTime.Parse(getDeliveryTime.Value.ToString()),
                    getCordenatorName.Value.ToString(), getDefenition.Value.ToString(), getTitle.Value.ToString(), getDeliveryUrl.Value.ToString(), getpublicdefenition.Value.ToString(), getprivatedefenition.Value.ToString());
                var validation = await _mediator.Send(downloadCommand);
                Dictionary<string, object> dictionaryToPassVariable = returnDictionary(delivery);
                dictionaryToPassVariable.Add("ok", validation.IsValid);
                camundaEngineClient.ExternalTaskService.Complete("FileLoading", externalTask.Id, dictionaryToPassVariable);
            });

            registerWorker("verify_files", async externalTask =>
            {
                var delivery = externalTask.Variables;
                var getId = returnVariableValue(delivery, "id");
                var getStudentName = returnVariableValue(delivery, "studentName");
                var getInstituteName = returnVariableValue(delivery, "instituteName");
                var getStudentNumber = returnVariableValue(delivery, "studentNumber");

                VerifyFilesCommand verifyFilesCommand = new VerifyFilesCommand(Guid.Parse(getId.Value.ToString()), string.Format(@"C:\Users\Carlos Campos\Desktop\Teste\Unzip\{0}_{1}_{2}.zip", getStudentNumber.Value.ToString(), getInstituteName.Value.ToString(),
               getStudentNumber.Value.ToString()));

                var validation = await _mediator.Send(verifyFilesCommand);
                Dictionary<string, object> dictionaryToPassVariable = returnDictionary(delivery);
                dictionaryToPassVariable["ok"] = validation.IsValid;
                camundaEngineClient.ExternalTaskService.Complete("FileLoading", externalTask.Id, dictionaryToPassVariable);
            });


            registerWorker("manual_processing", externalTask =>
            {
                var delivery = externalTask.Variables;
                var getStudentName = returnVariableValue(delivery, "studentName");
                var getDeliveryUrl = returnVariableValue(delivery, "fileUrl");
                var getInstituteName = returnVariableValue(delivery, "instituteName");
                var getStudentEmail = returnVariableValue(delivery, "studentEmail");
                var getCourseName = returnVariableValue(delivery, "courseName");
                var getStudentNumber = returnVariableValue(delivery, "studentNumber");

                _notificationService.SendEmail("carlosmiguelcampos1996@gmail.com", string.Format("Hello, <br/> Something went wrong on the delivery. Student Name:{0}, Institution Name: {1}, Student Number:{2}, Course Name:{3}. Thanks",
                    getStudentName.Value.ToString(), getInstituteName.Value.ToString(), getStudentNumber.Value.ToString(), getCourseName.Value.ToString()));
                Dictionary<string, object> dictionaryToPassVariable = returnDictionary(delivery);
                camundaEngineClient.ExternalTaskService.Complete("FileLoading", externalTask.Id, dictionaryToPassVariable);
            });
            pollingTimer = new Timer(_ => StartPolling(), null, 1, Timeout.Infinite);

        }

        private Dictionary<string, object> returnDictionary(Dictionary<string, Variable> delivery)
        {
            Dictionary<string, object> valuesforNextIteration = new Dictionary<string, object>();
            foreach (var item in delivery)
                valuesforNextIteration.Add(item.Key, item.Value.Value);
            return valuesforNextIteration;
        }

        private void registerWorker(string topicName, Action<ExternalTask> action)
        {
            workers.Add(topicName, action);
        }

        private void StartPolling()
        {
            PollTasks();
            pollingTimer.Change(1, Timeout.Infinite);
        }

        private void PollTasks()
        {
            try
            {
                var tasks = camundaEngineClient.ExternalTaskService.FetchAndLockTasks("FileLoading", 1000000, workers.Keys, 30000, null);
                Parallel.ForEach(
                    tasks,
                    new ParallelOptions { MaxDegreeOfParallelism = 1 },
                    (externalTask) =>
                    {
                        workers[externalTask.TopicName](externalTask);
                    });
            }
            catch (Exception ex)
            {
                Thread.Sleep(30000);
            }
        }
    }
}
