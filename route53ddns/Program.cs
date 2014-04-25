using Amazon.Route53;
using Amazon.Route53.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace route53ddns
{
    class Program
    {
        private static String currentIp;

        static void Main(string[] args)
        {
            String zoneid = ConfigurationManager.AppSettings["TargetZoneId"];
            String fqdn = ConfigurationManager.AppSettings["TargetFQDN"];

            Boolean isUpdate = true;

            // Init the client
            AmazonRoute53Client r53Client = new AmazonRoute53Client(Amazon.RegionEndpoint.USEast1);

            // List current result
            ListResourceRecordSetsRequest request = new ListResourceRecordSetsRequest();
            request.HostedZoneId = zoneid;
            request.StartRecordName = fqdn;
            request.StartRecordType = "A";
            request.MaxItems = "100";
            ListResourceRecordSetsResponse response = r53Client.ListResourceRecordSets(request);

            // Check whether the record is there
            if (response.ResourceRecordSets.Count > 0)
            {
                foreach (var record in response.ResourceRecordSets)
                {
                    if (record.Name.Equals(fqdn))
                    {
                        string oldIp = record.ResourceRecords.FirstOrDefault().Value;
                        if (oldIp.Equals(getCurrentIpAddress()))
                        {
                            isUpdate = false;
                        }
                        break;
                    }
                }
            }
            else
            {
                isUpdate = false;
            }

            if (isUpdate)
            {
                // Update existing one
                ResourceRecord objRecord = new ResourceRecord();
                objRecord.Value = getCurrentIpAddress();

                ResourceRecordSet objRecordSet = new ResourceRecordSet();
                objRecordSet.Name = fqdn;
                objRecordSet.Type = "A";
                objRecordSet.TTL = 300;
                objRecordSet.ResourceRecords.Add(objRecord);

                Change objChange = new Change();
                objChange.Action = ChangeAction.UPSERT;
                objChange.ResourceRecordSet = objRecordSet;

                List<Change> objChangeList = new List<Change>();
                objChangeList.Add(objChange);

                ChangeBatch objChangeBatch = new ChangeBatch();
                objChangeBatch.Changes = objChangeList;

                ChangeResourceRecordSetsRequest objRequest = new ChangeResourceRecordSetsRequest();
                objRequest.HostedZoneId = zoneid;
                objRequest.ChangeBatch = objChangeBatch;

                r53Client.ChangeResourceRecordSets(objRequest);
            }

            return;
        }

        private static String getCurrentIpAddress()
        {
            if (String.IsNullOrEmpty(currentIp))
            {
                String tmpString = new WebClient().DownloadString("http://curlmyip.com");
                currentIp = tmpString.Substring(0, tmpString.Length - 1);
            }
            return currentIp;
        }
    }
}
