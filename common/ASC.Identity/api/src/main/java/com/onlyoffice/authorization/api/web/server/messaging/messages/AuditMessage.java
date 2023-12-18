package com.onlyoffice.authorization.api.web.server.messaging.messages;

import com.onlyoffice.authorization.api.core.entities.Action;
import lombok.*;

import java.io.Serializable;
import java.sql.Timestamp;

@Builder
@Getter
@Setter
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
public class AuditMessage implements Message, Serializable {
    private String tag;
    private String initiator;
    private String target;
    private String ip;
    private String browser;
    private String platform;
    private Timestamp date;
    private int tenantId;
    private String userId;
    private String page;
    private Action actionEnum;
    private String description;
    @Builder.Default
    private AuditCommandCode commandCode = AuditCommandCode.LOG_AUDIT;
    public String getCode() {
        return commandCode.name();
    }
    public enum AuditCommandCode {
        LOG_AUDIT
    }
}
