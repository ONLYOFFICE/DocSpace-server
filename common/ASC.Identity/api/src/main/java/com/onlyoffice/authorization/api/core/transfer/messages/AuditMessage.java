package com.onlyoffice.authorization.api.core.transfer.messages;

import com.onlyoffice.authorization.api.core.entities.enums.Action;
import lombok.*;

import java.io.Serializable;
import java.sql.Timestamp;

@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
@EqualsAndHashCode
public class AuditMessage implements Serializable {
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
}
