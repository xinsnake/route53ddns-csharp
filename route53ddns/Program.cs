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
        static void Main(string[] args)
        {
            // Read the zone record
            string zoneid = ConfigurationManager.AppSettings["TargetZoneId"];
            string fqdn = ConfigurationManager.AppSettings["TargetFQDN"];

            // Get current ip address
            string currentIp = new WebClient().DownloadString("http://curlmyip.com");
            currentIp = currentIp.Substring(0, currentIp.Length - 1);
            Console.WriteLine("Current IP: " + currentIp);

            // Init the client
            AmazonRoute53Client r53Client = new AmazonRoute53Client(Amazon.RegionEndpoint.USEast1);

            // Construct the Object
            ChangeResourceRecordSetsRequest objRequest = new ChangeResourceRecordSetsRequest();
            objRequest.HostedZoneId = zoneid;
            Change objChange = new Change();
            objChange.Action = ChangeAction.CREATE; // this may change
            ResourceRecordSet objRecordSet = new ResourceRecordSet();
            objRecordSet.Name = fqdn;
            objRecordSet.Type = "A";
            objRecordSet.TTL = 300;
            ResourceRecord objRecord = new ResourceRecord();
            objRecord.Value = currentIp; // this may change
            objRecordSet.ResourceRecords.Add(objRecord);
            objChange.ResourceRecordSet = objRecordSet;
            List<Change> objChangeList = new List<Change>();
            objChangeList.Add(objChange);
            ChangeBatch objChangeBatch = new ChangeBatch();
            objChangeBatch.Changes = objChangeList;
            objRequest.ChangeBatch = objChangeBatch;
            // ChangeResourceRecordSetsResponse objResponse = r53Client.ChangeResourceRecordSets(objRequest);
            // Console.WriteLine("Got change ID: " + objResponse.ChangeInfo.Id);

            // List current result
            ListResourceRecordSetsRequest request = new ListResourceRecordSetsRequest();
            request.HostedZoneId = zoneid;
            request.StartRecordName = fqdn;
            request.StartRecordType = "A";
            request.MaxItems = "1";
            ListResourceRecordSetsResponse response = r53Client.ListResourceRecordSets(request);

            // Check whether the record is there
            if (response.ResourceRecordSets.Capacity > 0)
            {
                ResourceRecordSet rset = response.ResourceRecordSets.FirstOrDefault();
                var firstr = rset.ResourceRecords.FirstOrDefault();
                string oldIp = firstr.Value;
                Console.WriteLine("Found current A record contains IP: " + oldIp);

                if (oldIp.Equals(currentIp))
                {
                    // nothing to do
                    Console.WriteLine("Current IP is the same as A record, exit.");
                    return;
                }
                else
                {
                    Console.WriteLine("Current IP is different from A record, update required.");
                    
                    // Delete old one
                    Console.Write("Deleting the old record...");
                    objRequest.ChangeBatch.Changes.First().Action = ChangeAction.DELETE;
                    objRequest.ChangeBatch.Changes.First().ResourceRecordSet.ResourceRecords.First().Value = oldIp;
                    ChangeResourceRecordSetsResponse deleteR = r53Client.ChangeResourceRecordSets(objRequest);
                    Console.WriteLine("Done. Got delete ID: " + deleteR.ChangeInfo.Id);
                }
            }
            else
            {
                Console.WriteLine("No record found, create required.");
            }

            // Add the new one
            Console.Write("Write current IP...");

            objRequest.ChangeBatch.Changes.First().Action = ChangeAction.CREATE;
            objRequest.ChangeBatch.Changes.First().ResourceRecordSet.ResourceRecords.First().Value = currentIp;
            ChangeResourceRecordSetsResponse changeR = r53Client.ChangeResourceRecordSets(objRequest);
            Console.WriteLine("Done. Got change ID: " + changeR.ChangeInfo.Id);
        }
    }
}
