module EmailClient
open System
open MailKit
open MailKit.Net.Smtp
open MimeKit

type EmailConfig = {
    SmtpServer: string
    SmtpPort: int
    Username: string
    Password: string
}

let sendEmail config subject (body: string) (recipients: string list) =
    use client = new SmtpClient()
    client.ServerCertificateValidationCallback <- fun s c h e -> true
    client.Connect(config.SmtpServer, config.SmtpPort, true)
    printfn "%A" config
    client.Authenticate(config.Username, config.Password)

    let msg = new MimeMessage()

    msg.From.Add(MailboxAddress config.Username)
    for r in recipients do
        msg.To.Add(MailboxAddress r)
    msg.Subject <- subject
    msg.Body <-
        let tp = TextPart("plain")
        tp.Text <- body
        tp
    client.Send msg

