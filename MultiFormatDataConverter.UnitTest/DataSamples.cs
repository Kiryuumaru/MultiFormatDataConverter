﻿using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;

namespace MultiFormatDataConverter.UnitTest;

public static class DataSamples
{
    public const string XmlSample = """
        <root>
          <StringProp>Hello</StringProp>
          <IntProp>11</IntProp>
          <LongProp>123456789012345678</LongProp>
          <DecimalProp>12345.67</DecimalProp>
          <DoubleProp>1.2345</DoubleProp>
          <BoolTrue>true</BoolTrue>
          <BoolFalse>false</BoolFalse>
          <NullProp xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
          <ArgedProp xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <xsi:ArgedStringProp>Hello arged</xsi:ArgedStringProp>
            <xsi:ArgedIntProp>22</xsi:ArgedIntProp>
            <StringProp>Hello arged</StringProp>
            <IntProp>22</IntProp>
            <LongProp>234567890123456789</LongProp>
            <DecimalProp>23456.78</DecimalProp>
            <DoubleProp>2.3456</DoubleProp>
            <BoolTrueProp>true</BoolTrueProp>
            <BoolFalseProp>false</BoolFalseProp>
            <NullProp xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
          </ArgedProp>
          <ArrayPropSimple>Item1</ArrayPropSimple>
          <ArrayPropSimple>Item2</ArrayPropSimple>
          <ArrayPropMixedTypes>Item1</ArrayPropMixedTypes>
          <ArrayPropMixedTypes>44</ArrayPropMixedTypes>
          <ArrayPropMixedTypes>337206854775807922</ArrayPropMixedTypes>
          <ArrayPropMixedTypes>45678.90</ArrayPropMixedTypes>
          <ArrayPropMixedTypes>4.5678</ArrayPropMixedTypes>
          <ArrayPropComplex>
            <Item1></Item1>
            <StringProp>Hello sub selection array Item1</StringProp>
            <IntProp>55</IntProp>
            <LongProp>567890123456781234</LongProp>
            <DecimalProp>56789.01</DecimalProp>
            <DoubleProp>5.6789</DoubleProp>
            <BoolTrueProp>false</BoolTrueProp>
            <BoolFalseProp>true</BoolFalseProp>
            <NullProp xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
          </ArrayPropComplex>
          <ArrayPropComplex>
            <Item2></Item2>
            <StringProp>Hello sub selection array Item2</StringProp>
            <IntProp>66</IntProp>
            <LongProp>678901234567812345</LongProp>
            <DecimalProp>67890.12</DecimalProp>
            <DoubleProp>6.7890</DoubleProp>
            <BoolTrueProp>false</BoolTrueProp>
            <BoolFalseProp>true</BoolFalseProp>
            <NullProp xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
          </ArrayPropComplex>
          <SubSection>
            <StringProp>Hello from sub selection</StringProp>
            <IntProp>44</IntProp>
            <LongProp>456789012345678123</LongProp>
            <DecimalProp>45678.90</DecimalProp>
            <DoubleProp>4.5678</DoubleProp>
            <BoolTrueProp>false</BoolTrueProp>
            <BoolFalseProp>true</BoolFalseProp>
            <NullProp xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
            <ArrayPropSimple>Item1</ArrayPropSimple>
            <ArrayPropSimple>Item2</ArrayPropSimple>
            <ArrayPropMixedTypes>Item1</ArrayPropMixedTypes>
            <ArrayPropMixedTypes>44</ArrayPropMixedTypes>
            <ArrayPropMixedTypes>337206854775807922</ArrayPropMixedTypes>
            <ArrayPropMixedTypes>45678.90</ArrayPropMixedTypes>
            <ArrayPropMixedTypes>4.5678</ArrayPropMixedTypes>
            <ArrayPropComplex>
              <Item1></Item1>
              <StringProp>Hello sub selection array Item1</StringProp>
              <IntProp>55</IntProp>
              <LongProp>567890123456781234</LongProp>
              <DecimalProp>56789.01</DecimalProp>
              <DoubleProp>5.6789</DoubleProp>
              <BoolTrueProp>false</BoolTrueProp>
              <BoolFalseProp>true</BoolFalseProp>
              <NullProp xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
            </ArrayPropComplex>
            <ArrayPropComplex>
              <Item2></Item2>
              <StringProp>Hello sub selection array Item2</StringProp>
              <IntProp>66</IntProp>
              <LongProp>678901234567812345</LongProp>
              <DecimalProp>67890.12</DecimalProp>
              <DoubleProp>6.7890</DoubleProp>
              <BoolTrueProp>false</BoolTrueProp>
              <BoolFalseProp>true</BoolFalseProp>
              <NullProp xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
            </ArrayPropComplex>
          </SubSection>
        </root>
        """;

    public const string YamlSample = """
        StringProp: Hello
        IntProp: 11
        LongProp: 123456789012345678
        DecimalProp: 12345.67
        DoubleProp: 1.2345
        BoolTrue: true
        BoolFalse: false
        NullProp: null
        ArrayPropSimple:
        - Item1
        - Item2
        ArrayPropMixedTypes:
        - Item1
        - 44
        - 337206854775807922
        - 45678.90
        - 4.5678
        ArrayPropComplex:
        - Item1:
          StringProp: Hello from simple Item1
          IntProp: 22
          LongProp: 234567890123456781
          DecimalProp: 23456.78
          DoubleProp: 2.3456
          BoolTrueProp: false
          BoolFalseProp: true
          NullProp: null
        - Item2:
          StringProp: Hello from simple Item2
          IntProp: 33
          LongProp: 345678901234567812
          DecimalProp: 34567.89
          DoubleProp: 3.4567
          BoolTrueProp: false
          BoolFalseProp: true
          NullProp: null
        SubSection:
          StringProp: Hello from sub selection
          IntProp: 44
          LongProp: 456789012345678123
          DecimalProp: 45678.90
          DoubleProp: 4.5678
          BoolTrueProp: false
          BoolFalseProp: true
          NullProp: null
          ArrayPropSimple:
          - Item1
          - Item2
          ArrayPropMixedTypes:
          - Item1
          - 44
          - 337206854775807922
          - 45678.90
          - 4.5678
          ArrayPropComplex:
          - Item1:
            StringProp: Hello sub selection array Item1
            IntProp: 55
            LongProp: 567890123456781234
            DecimalProp: 56789.01
            DoubleProp: 5.6789
            BoolTrueProp: false
            BoolFalseProp: true
            NullProp: null
          - Item2:
            StringProp: Hello sub selection array Item2
            IntProp: 66
            LongProp: 678901234567812345
            DecimalProp: 67890.12
            DoubleProp: 6.7890
            BoolTrueProp: false
            BoolFalseProp: true
            NullProp: null
        """;

    public const string JsonSample = """
        {
          "StringProp": "Hello",
          "IntProp": 11,
          "LongProp": 123456789012345678,
          "DecimalProp": 12345.67,
          "DoubleProp": 1.2345,
          "BoolTrue": true,
          "BoolFalse": false,
          "NullProp": null,
          "ArrayPropSimple": [
            "Item1",
            "Item2"
          ],
          "ArrayPropMixedTypes": [
            "Item1",
            44,
            337206854775808000,
            45678.9,
            4.5678
          ],
          "ArrayPropComplex": [
            {
              "Item1": null,
              "StringProp": "Hello from simple Item1",
              "IntProp": 22,
              "LongProp": 234567890123456781,
              "DecimalProp": 23456.78,
              "DoubleProp": 2.3456,
              "BoolTrueProp": false,
              "BoolFalseProp": true,
              "NullProp": null
            },
            {
              "Item2": null,
              "StringProp": "Hello from simple Item2",
              "IntProp": 33,
              "LongProp": 345678901234567812,
              "DecimalProp": 34567.89,
              "DoubleProp": 3.4567,
              "BoolTrueProp": false,
              "BoolFalseProp": true,
              "NullProp": null
            }
          ],
          "SubSection": {
            "StringProp": "Hello from sub selection",
            "IntProp": 44,
            "LongProp": 456789012345678123,
            "DecimalProp": 45678.9,
            "DoubleProp": 4.5678,
            "BoolTrueProp": false,
            "BoolFalseProp": true,
            "NullProp": null,
            "ArrayPropSimple": [
              "Item1",
              "Item2"
            ],
            "ArrayPropMixedTypes": [
              "Item1",
              44,
              337206854775808000,
              45678.9,
              4.5678
            ],
            "ArrayPropComplex": [
              {
                "Item1": null,
                "StringProp": "Hello sub selection array Item1",
                "IntProp": 55,
                "LongProp": 567890123456781234,
                "DecimalProp": 56789.01,
                "DoubleProp": 5.6789,
                "BoolTrueProp": false,
                "BoolFalseProp": true,
                "NullProp": null
              },
              {
                "Item2": null,
                "StringProp": "Hello sub selection array Item2",
                "IntProp": 66,
                "LongProp": 678901234567812345,
                "DecimalProp": 67890.12,
                "DoubleProp": 6.789,
                "BoolTrueProp": false,
                "BoolFalseProp": true,
                "NullProp": null
              }
            ]
          }
        }
        """;
}
