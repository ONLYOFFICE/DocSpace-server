/**
 *
 */
package com.onlyoffice.authorization.web.transfer.messaging.wrappers;

import com.rabbitmq.client.Channel;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.io.Serializable;

/**
 *
 * @param <E>
 */
@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class MessageWrapper<E> implements Serializable {
    private long tag;
    private Channel channel;
    private E data;
}
